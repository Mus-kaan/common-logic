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
using Microsoft.Rest.Azure;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IO;
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
            LogContext.PushProperty(nameof(options.ImageVersion), options.ImageVersion);
            logger.LogProcessStart();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                var content = File.ReadAllText(_options.ConfigPath);
                BuilderOptions config = content.FromJson<BuilderOptions>();

                File.WriteAllText("subscription-id.txt", config.SubscriptionId.ToString());
                if (_options.OutputSubscriptionIdOnly)
                {
                    _appLifetime.StopApplication();
                    return;
                }

                LogContext.PushProperty(nameof(config.Tenant), config.Tenant.ToString());
                LogContext.PushProperty(nameof(config.SubscriptionId), config.SubscriptionId.ToString());
                LogContext.PushProperty(nameof(config.Location), config.Location.Name);
                LogContext.PushProperty(nameof(config.ResourceGroupName), config.ResourceGroupName);
                LogContext.PushProperty(nameof(config.ImageGalleryName), config.ImageGalleryName);
                LogContext.PushProperty(nameof(config.PackerVMSize), config.PackerVMSize);
                LogContext.PushProperty(nameof(config.ImageReplicationRegions), config.ImageReplicationRegions.ToJson());

                if (!File.Exists(_options.ArtifactPath))
                {
                    var errMsg = $"Cannot find the artifact package in the location : {_options.ArtifactPath}";
                    _logger.Error(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                string tenantId = null;
                switch (config.Tenant)
                {
                    case TenantType.MS:
                        {
                            tenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
                            break;
                        }

                    case TenantType.AME:
                        {
                            tenantId = "33e01921-4d64-4f8c-a055-5bdaffd5e33d";
                            break;
                        }

                    default:
                        throw new InvalidOperationException($"Does not support tenant: {config.Tenant}");
                }

                if (!string.IsNullOrEmpty(_options.RunnerSPNObjectId))
                {
                    config.ExecutorSPNObjectId = _options.RunnerSPNObjectId;
                }

                TokenCredential tokenCredential = null;
                Func<AzureCredentials> azureCredentialsProvider = null;
                KeyVaultClient kvClient = null;

                if (!string.IsNullOrEmpty(_options.AuthFile))
                {
                    _logger.Information("Use auth json file to authenticate against Azure.");
                    var authContract = AuthFileContract.FromFile(_options.AuthFile);

                    config.ExecutorSPNObjectId = authContract.ServicePrincipalObjectId;
                    config.SubscriptionId = Guid.Parse(authContract.SubscriptionId);
                    kvClient = KeyVaultClientFactory.FromClientIdAndSecret(authContract.ClientId, authContract.ClientSecret);
                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory.FromFile(_options.AuthFile);
                    tokenCredential = new ClientSecretCredential(authContract.TenantId, authContract.ClientId, authContract.ClientSecret);
                }
                else
                {
                    _logger.Information("Use Managed Identity to authenticate against Azure.");
                    kvClient = KeyVaultClientFactory.FromMSI();

                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory
                    .FromMSI(new MSILoginInformation(MSIResourceType.VirtualMachine), AzureEnvironment.AzureGlobalCloud, tenantId)
                    .WithDefaultSubscription(config.SubscriptionId.ToString());
                    tokenCredential = new ManagedIdentityCredential();
                }

                LiftrAzureFactory azFactory = new LiftrAzureFactory(
                    _logger,
                    tenantId,
                    config.ExecutorSPNObjectId,
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

        private async Task RunActionAsync(KeyVaultClient kvClient, BuilderOptions config, LiftrAzureFactory azFactory, CancellationToken cancellationToken)
        {
            _logger.Information("BuilderCommandOptions: {@BuilderCommandOptions}", _options);
            _logger.Information("Parsed config file: {@BuilderOptions}", config);

            using (var operation = _logger.StartTimedOperation("RunImageBuilder"))
            {
                _logger.Information("Current correlation Id is: {correlationId}", TelemetryContext.GetOrGenerateCorrelationId());
                _logger.Information("You can use correlation Id '{correlationId}' to query all the related ARM logs and Azure Image Builder logs.", TelemetryContext.GetOrGenerateCorrelationId());

                try
                {
                    var tags = new Dictionary<string, string>()
                    {
                        [NamingContext.c_createdAtTagName] = _timeSource.UtcNow.ToZuluString(),
                    };

                    ImageBuilderOrchestrator orchestrator = new ImageBuilderOrchestrator(
                        config,
                        azFactory,
                        kvClient,
                        _timeSource,
                        _logger);

                    await orchestrator.CreateOrUpdateLiftrImageBuilderInfrastructureAsync(tags);

                    var generatedBuilderTemplate = await orchestrator.BuildCustomizedSBIAsync(
                        _options.ImageName,
                        _options.ImageVersion,
                        _options.SourceImage,
                        _options.ArtifactPath,
                        tags,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is CloudException)
                    {
                        var cloudEx = ex as CloudException;
                        _logger.Fatal(ex, "Failed with CloudException. Status code: {statusCode}, Response: {@response}, Request: {@request}", cloudEx.Response.StatusCode, cloudEx.Response, cloudEx.Request);
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
