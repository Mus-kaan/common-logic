//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Logging;
using Serilog.Context;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public sealed class ActionExecutor : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IApplicationLifetime _appLifetime;
        private readonly BuilderCommandOptions _options;
        private readonly EnvironmentOptions _envOptions;
        private readonly ITimeSource _timeSource;

        public ActionExecutor(
            Serilog.ILogger logger,
            IApplicationLifetime appLifetime,
            BuilderCommandOptions options,
            IOptions<EnvironmentOptions> envOptions,
            ITimeSource timeSource)
        {
            if (envOptions == null)
            {
                throw new ArgumentNullException(nameof(envOptions));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _envOptions = envOptions.Value;
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));

            LogContext.PushProperty("TargetSubscriptionId", options.SubscriptionId);
            LogContext.PushProperty("ExeAction", options.Action);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            _logger.Information("Start ActionExecutor ...");

            try
            {
                if (_envOptions.ArtifactOptions == null)
                {
                    throw new InvalidOperationException("Please sepcify ArtifactOptions.");
                }

                if (string.IsNullOrEmpty(_options.SubscriptionId))
                {
                    throw new InvalidOperationException("Please sepcify a valid Subscription Id.");
                }

                if (!File.Exists(_options.ConfigPath))
                {
                    var errMsg = $"Config json file doesn't exist at the path: {_options.ConfigPath}";
                    _logger.Fatal(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                _logger.Information("ActionExecutor Action:{ExeAction}", _options.Action);
                _logger.Information("BuilderCommandOptions: {@BuilderCommandOptions}", _options);

                if (_options.Action == ActionType.UploadArtifactForImageBuilder)
                {
                    if (!File.Exists(_options.ArtifactPath))
                    {
                        var errMsg = $"Cannot find the artifact package in the location : {_options.ArtifactPath}";
                        _logger.Error(errMsg);
                        throw new InvalidOperationException(errMsg);
                    }

                    if (!File.Exists(_options.AIBTemplatePath))
                    {
                        var errMsg = $"Cannot find the Azure Image Builder template file at the location : {_options.AIBTemplatePath}";
                        _logger.Error(errMsg);
                        throw new InvalidOperationException(errMsg);
                    }

                    if (!File.Exists(_options.ImageMetaPath))
                    {
                        var errMsg = $"Cannot find the 'image-meta.json' at the location : {_options.ImageMetaPath}";
                        _logger.Error(errMsg);
                        throw new InvalidOperationException(errMsg);
                    }
                }

                Func<AzureCredentials> azureCredentialsProvider = null;
                KeyVaultClient kvClient = null;
                if (!string.IsNullOrEmpty(_options.AuthFile))
                {
                    _logger.Information("Use auth json file to authenticate against Azure.");
                    var authContract = AuthFileContract.FromFile(_options.AuthFile);
                    kvClient = KeyVaultClientFactory.FromClientIdAndSecret(authContract.ClientId, authContract.ClientSecret);

                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory.FromFile(_options.AuthFile);
                }
                else
                {
                    _logger.Information("Use MSI to authenticate against Azure.");
                    kvClient = KeyVaultClientFactory.FromMSI();

                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory
                    .FromMSI(new MSILoginInformation(MSIResourceType.VirtualMachine), AzureEnvironment.AzureGlobalCloud, _envOptions.TenantId)
                    .WithDefaultSubscription(_options.SubscriptionId);
                }

                LiftrAzureFactory azFactory = new LiftrAzureFactory(_logger, _options.SubscriptionId, azureCredentialsProvider);

                _ = RunActionAsync(kvClient, azFactory);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "ActionExecutor StartAsync Failed.");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Stop ActionExecutor ...");
            return Task.CompletedTask;
        }

        private async Task RunActionAsync(KeyVaultClient kvClient, LiftrAzureFactory azFactory)
        {
            await Task.Yield();
            using (var operation = _logger.StartTimedOperation(_options.Action.ToString()))
            {
                try
                {
                    var content = File.ReadAllText(_options.ConfigPath);
                    GalleryOptions config = content.FromJson<GalleryOptions>();

                    operation.SetContextProperty(nameof(config.PartnerName), config.PartnerName);
                    operation.SetContextProperty(nameof(config.Environment), config.Environment.ToString());
                    operation.SetContextProperty(nameof(config.Location), config.LocationStr);
                    operation.SetContextProperty(nameof(config.GalleryBaseName), config.GalleryBaseName);

                    _logger.Information("Parsed config file: {@config}", config);

                    var namingContext = new NamingContext(config.PartnerName, config.ShortPartnerName, config.Environment, config.Location);
                    namingContext.Tags["FirstCreatedAt"] = DateTime.UtcNow.ToZuluString();

                    var rgName = namingContext.ResourceGroupName(config.GalleryBaseName);
                    var galleryName = $"{namingContext.ShortPartnerName}_{config.GalleryBaseName}_sig";
                    var storageAccountName = $"st{namingContext.ShortPartnerName}{config.GalleryBaseName}".ToLowerInvariant();
                    var kvName = namingContext.KeyVaultName(config.GalleryBaseName);
                    ImageBuilderOrchestrator orchestrator = new ImageBuilderOrchestrator(
                        _envOptions,
                        azFactory,
                        namingContext,
                        rgName,
                        galleryName,
                        config.ImageName,
                        storageAccountName,
                        _timeSource,
                        _logger);

                    if (_options.Action == ActionType.CreateOrUpdateImageGalleryResources)
                    {
                        await orchestrator.CreateOrUpdateInfraAsync(kvName);
                    }
                    else if (_options.Action == ActionType.UploadArtifactForImageBuilder)
                    {
                        var generatedBuilderTemplate = await orchestrator.UploadArtifactAndPrepareBuilderTemplateAsync(
                            _envOptions.ArtifactOptions,
                            _options.ArtifactPath,
                            _options.ImageMetaPath,
                            _options.AIBTemplatePath,
                            _envOptions.BaseSBIVerion);

                        File.WriteAllText("AIB-template.json", generatedBuilderTemplate);
                        _logger.Information("Wrote the generated template in file: AIB-template.json");
                    }
                    else if (_options.Action == ActionType.MoveSBIToOurStorage)
                    {
                        await orchestrator.MoveSBIToOurStorageAsync(kvName, kvClient);
                    }

                    File.WriteAllText("ImageBuilderResourceGroupName.txt", rgName);
                    File.WriteAllText("ImageBuilderGalleryName.txt", galleryName);
                    File.WriteAllText("ImageBuilderStorageAccountName.txt", storageAccountName);

                    _logger.Information("----------------------------------------------------------------------");
                    _logger.Information("Finished successfully!");
                    _logger.Information("----------------------------------------------------------------------");
                }
                catch (Exception ex)
                {
                    _logger.Fatal(ex, "Failed.");
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
