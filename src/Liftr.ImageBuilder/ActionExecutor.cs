//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Liftr.Utilities;
using Microsoft.Rest.Azure;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public sealed class ActionExecutor : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly BuilderCommandOptions _options;
        private readonly ITimeSource _timeSource;

        public ActionExecutor(
            Serilog.ILogger logger,
            IHostApplicationLifetime appLifetime,
            BuilderCommandOptions options,
            ITimeSource timeSource)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));

            LogContext.PushProperty(nameof(options.SourceImage), options.SourceImage.ToString());
            LogContext.PushProperty(nameof(options.ImageName), options.ImageName);
            LogContext.PushProperty("PartnerName", options.ImageName); // ImageBuilder does not have a 'PartnerName' concept, this is for reuse the existing dashboard.
            logger.LogProcessStart();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                if (Version.TryParse(_options.ImageVersion, out var parsedVersion))
                {
                    _options.ImageVersion = parsedVersion.ToString();
                }
                else
                {
                    throw new InvalidImageVersionException($"The image version value '{_options.ImageVersion}' is invalid. ");
                }

                LogContext.PushProperty(nameof(_options.ImageVersion), _options.ImageVersion);

                var content = File.ReadAllText(_options.ConfigPath);
                BuilderOptions config = content.FromJson<BuilderOptions>();

                File.WriteAllText("subscription-id.txt", config.SubscriptionId.ToString());
                if (_options.OutputSubscriptionIdOnly)
                {
                    _appLifetime.StopApplication();
                    return;
                }

                LogContext.PushProperty(nameof(config.SubscriptionId), config.SubscriptionId.ToString());
                LogContext.PushProperty(nameof(config.Location), config.Location.Name);
                LogContext.PushProperty(nameof(config.ResourceGroupName), config.ResourceGroupName);
                LogContext.PushProperty(nameof(config.ImageGalleryName), config.ImageGalleryName);
                LogContext.PushProperty(nameof(config.PackerVMSize), config.PackerVMSize);
                LogContext.PushProperty(nameof(config.ImageReplicationRegions), config.ImageReplicationRegions.ToJson());

                ValidateOptions(config);

                var executingSPNObjectId = _options.RunnerSPNObjectId;

                TokenCredential tokenCredential = null;
                Func<AzureCredentials> azureCredentialsProvider = null;
                KeyVaultClient kvClient = null;

                string tenantId = null;

                if (!string.IsNullOrEmpty(_options.AuthFile))
                {
                    _logger.Information("Use auth json file to authenticate against Azure.");
                    var authContract = AuthFileContract.FromFile(_options.AuthFile);

                    if (!string.IsNullOrEmpty(authContract.ServicePrincipalObjectId))
                    {
                        executingSPNObjectId = authContract.ServicePrincipalObjectId;
                    }

                    tenantId = authContract.TenantId;
                    config.SubscriptionId = Guid.Parse(authContract.SubscriptionId);
                    kvClient = KeyVaultClientFactory.FromClientIdAndSecret(authContract.ClientId, authContract.ClientSecret);
                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory.FromFile(_options.AuthFile);

                    TokenCredentialOptions options = new TokenCredentialOptions()
                    {
                        AuthorityHost = new Uri(authContract.ActiveDirectoryEndpointUrl),
                    };
                    tokenCredential = new ClientSecretCredential(authContract.TenantId, authContract.ClientId, authContract.ClientSecret, options);
                }
                else
                {
                    _logger.Information("Use Managed Identity to authenticate against Azure.");
                    kvClient = KeyVaultClientFactory.FromMSI();

                    var azEnv = _options.Cloud.LoadAzEnvironment();
                    TokenCredentialOptions tokenCredentialOptions = new TokenCredentialOptions()
                    {
                        AuthorityHost = new Uri(azEnv.AuthenticationEndpoint),
                    };

                    using TenantHelper tenantHelper = new TenantHelper(new Uri(azEnv.ResourceManagerEndpoint));
                    tenantId = await tenantHelper.GetTenantIdForSubscriptionAsync(config.SubscriptionId.ToString());

                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory
                    .FromMSI(new MSILoginInformation(MSIResourceType.VirtualMachine), azEnv, tenantId)
                    .WithDefaultSubscription(config.SubscriptionId.ToString());
                    tokenCredential = new ManagedIdentityCredential(options: tokenCredentialOptions);
                }

                LogContext.PushProperty(nameof(tenantId), tenantId);

                LiftrAzureFactory azFactory = new LiftrAzureFactory(
                    _logger,
                    tenantId,
                    executingSPNObjectId,
                    config.SubscriptionId.ToString(),
                    tokenCredential,
                    azureCredentialsProvider);

                // fire and forget.
                _ = RunActionAsync(kvClient, config, azFactory, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "ActionExecutor StartAsync Failed.");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void ValidateOptions(BuilderOptions config)
        {
            if (_options.Action == ActionType.BakeNewVersion)
            {
                if (string.IsNullOrEmpty(_options.ArtifactPath))
                {
                    var ex = new InvalidOperationException("Please make sure you provided the artifact file path");
                    _logger.Fatal(ex.Message);
                    throw ex;
                }

                if (!File.Exists(_options.ArtifactPath))
                {
                    var errMsg = $"Cannot find the artifact package in the location : {_options.ArtifactPath}";
                    _logger.Fatal(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                if (_options.SourceImage == null)
                {
                    var ex = new InvalidOperationException("Please make sure you provided Source image type, one of: [WindowsServer2016Datacenter, WindowsServer2016DatacenterCore, WindowsServer2016DatacenterContainers, WindowsServer2019Datacenter, WindowsServer2019DatacenterCore, WindowsServer2019DatacenterContainers, U1604LTS, U1604FIPS, U1804LTS]");
                    _logger.Fatal(ex.Message);
                    throw ex;
                }
            }
        }

        private async Task RunActionAsync(KeyVaultClient kvClient, BuilderOptions config, LiftrAzureFactory azFactory, CancellationToken cancellationToken)
        {
            _logger.Information("BuilderCommandOptions: {@BuilderCommandOptions}", _options);
            _logger.Information("Parsed config file: {@BuilderOptions}", config);

            using (var operation = _logger.StartTimedOperation("RunImageBuilder"))
            {
                _logger.Information("Current correlation Id is: {correlationId}", TelemetryContext.GetOrGenerateCorrelationId());
                _logger.Information("You can use correlation Id '{correlationId}' to query all the related ARM logs and Azure Image Builder logs.", TelemetryContext.GetOrGenerateCorrelationId());

                operation.SetProperty(nameof(_options.ImageName), _options.ImageName);
                operation.SetProperty(nameof(_options.ImageVersion), _options.ImageVersion);
                operation.SetProperty(nameof(config.ImageGalleryName), config.ImageGalleryName);
                operation.SetProperty(nameof(config.ResourceGroupName), config.ResourceGroupName);
                operation.SetProperty(nameof(config.Location), config.Location.Name);
                operation.SetProperty(nameof(config.SubscriptionId), config.SubscriptionId.ToString());
                operation.SetProperty(nameof(config.ImageReplicationRegions), string.Join(", ", config.ImageReplicationRegions.Select(r => r.Name)));

                try
                {
                    var tags = new Dictionary<string, string>()
                    {
                        [NamingContext.c_createdAtTagName] = _timeSource.UtcNow.ToZuluString(),
                        [NamingContext.c_versionTagName] = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion,
                    };

                    ImageBuilderOrchestrator orchestrator = new ImageBuilderOrchestrator(
                        config,
                        azFactory,
                        kvClient,
                        _timeSource,
                        _logger);

                    InfrastructureType infraType = InfrastructureType.ImportImage;
                    if (_options.Action == ActionType.BakeNewVersion)
                    {
                        infraType = config.ExportVHDToStorage ? InfrastructureType.BakeNewImageAndExport : InfrastructureType.BakeNewImage;
                    }

                    (var kv, var gallery, var artifactStore, var exportStorageAccount) = await orchestrator.CreateOrUpdateLiftrImageBuilderInfrastructureAsync(infraType, _options.SourceImage, tags);
                    var extensionParameters = new CallbackParameters()
                    {
                        LiftrAzureFactory = azFactory,
                        KeyVaultClient = kvClient,
                        Logger = _logger,
                        BuilderOptions = config,
                        BuilderCommandOptions = _options,
                        KeyVault = kv,
                        VHDStorageAccount = exportStorageAccount,
                        Gallery = gallery,
                    };

                    if (_options.Action == ActionType.BakeNewVersion)
                    {
                        (var imgVersion, var vhdUri) = await orchestrator.BuildCustomizedSBIAsync(
                            _options.ImageName,
                            _options.ImageVersion,
                            _options.SourceImage.Value,
                            _options.ArtifactPath,
                            tags,
                            cancellationToken);

                        extensionParameters.ImageVersion = imgVersion;
                        extensionParameters.VHDUri = vhdUri;

                        if (ImageBuilderExtension.AfterBakeImageAsync != null)
                        {
                            using (_logger.StartTimedOperation("ImageBuilderExtension.AfterBakeImageAsync"))
                            {
                                await ImageBuilderExtension.AfterBakeImageAsync.Invoke(extensionParameters);
                            }
                        }
                    }
                    else if (_options.Action == ActionType.ImportOneVersion)
                    {
                        using var kvValet = new KeyVaultConcierge(kv.VaultUri, kvClient, _logger);
                        var imgImporter = new ImageImporter(
                            config,
                            artifactStore,
                            azFactory,
                            kvValet,
                            _timeSource,
                            _logger);

                        var imgVersion = await imgImporter.ImportImageVHDAsync(_options.ImageName, _options.ImageVersion, cancellationToken);
                        _logger.Information("The imported image version can be found at Shared Image Gallery Image version resource Id: {sigVerionId}", imgVersion.Id);

                        extensionParameters.ImageVersion = imgVersion;

                        if (ImageBuilderExtension.AfterImportImageAsync != null)
                        {
                            using (_logger.StartTimedOperation("ImageBuilderExtension.AfterImportImageAsync"))
                            {
                                await ImageBuilderExtension.AfterImportImageAsync.Invoke(extensionParameters);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is CredentialUnavailableException)
                    {
                        _logger.Fatal(ex, "If this is running inside EV2, please redeploy the release step. The EV2 ACI might be having some transient issues.");
                    }
                    else if (ex is CloudException)
                    {
                        var cloudEx = ex as CloudException;
                        _logger.Fatal(ex, "Failed with CloudException. Status code: {statusCode}, Response: {@response}, Request: {requestUri}", cloudEx.Response.StatusCode, cloudEx.Response, cloudEx.Request.RequestUri.ToString());
                    }
                    else
                    {
                        _logger.Fatal(ex, "Failed.");
                    }

                    Environment.ExitCode = -1;
                    operation.FailOperation();
                    throw;
                }
                finally
                {
                    _appLifetime.StopApplication();
                }
            }
        }
    }
}
