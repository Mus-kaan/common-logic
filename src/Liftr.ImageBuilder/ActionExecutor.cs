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
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
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
            logger.LogProcessStart();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                _logger.Information("BuilderCommandOptions: {@BuilderCommandOptions}", _options);

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

                _logger.Information("Parsed config file: {BuilderOptions}", config);

                if (!File.Exists(_options.ArtifactPath))
                {
                    var errMsg = $"Cannot find the artifact package in the location : {_options.ArtifactPath}";
                    _logger.Error(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                string tenantId = null;
                string azureVMImageBuilderObjectId = null;
                switch (config.Tenant)
                {
                    case TenantType.MS:
                        {
                            tenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
                            azureVMImageBuilderObjectId = "ef511139-6170-438e-a6e1-763dc31bdf74";
                            break;
                        }

                    case TenantType.AME:
                        {
                            tenantId = "33e01921-4d64-4f8c-a055-5bdaffd5e33d";
                            azureVMImageBuilderObjectId = "cc22e29d-20f4-457d-87dd-aea1bdcce16a";
                            break;
                        }

                    default:
                        throw new InvalidOperationException($"Does not support tenant: {config.Tenant.ToString()}");
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
                    _logger.Information("Use MSI to authenticate against Azure.");
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
                _ = RunActionAsync(azureVMImageBuilderObjectId, kvClient, config, azFactory, cancellationToken);
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

        private async Task RunActionAsync(string azureVMImageBuilderObjectId, KeyVaultClient kvClient, BuilderOptions config, LiftrAzureFactory azFactory, CancellationToken cancellationToken)
        {
            await Task.Yield();
            using (var operation = _logger.StartTimedOperation("RunImageBuilder"))
            {
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

                    await orchestrator.CreateOrUpdateInfraAsync(azureVMImageBuilderObjectId, tags);

                    var generatedBuilderTemplate = await orchestrator.BuildCustomizedSBIAsync(
                        _options.ImageName,
                        _options.ImageVersionTag,
                        _options.SourceImage,
                        _options.ArtifactPath,
                        tags,
                        cancellationToken);
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
