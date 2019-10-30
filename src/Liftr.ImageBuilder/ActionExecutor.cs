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
using Microsoft.Liftr.KeyVault;
using Microsoft.Liftr.Logging;
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

                if (_options.Action == ActionType.GenerateCustomizedSBI)
                {
                    if (!File.Exists(_options.ArtifactPath))
                    {
                        var errMsg = $"Cannot find the artifact package in the location : {_options.ArtifactPath}";
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

                _ = RunActionAsync(kvClient, azFactory, cancellationToken);
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

        private async Task RunActionAsync(KeyVaultClient kvClient, LiftrAzureFactory azFactory, CancellationToken cancellationToken)
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

                    var rgName = namingContext.ResourceGroupName(config.GalleryBaseName);
                    var galleryName = namingContext.SharedImageGalleryName(config.GalleryBaseName);
                    var storageAccountName = namingContext.StorageAccountName(config.GalleryBaseName);
                    var kvName = namingContext.KeyVaultName(config.GalleryBaseName);
                    ImageBuilderOptions imgOptions = new ImageBuilderOptions()
                    {
                        ResourceGroupName = rgName,
                        GalleryName = galleryName,
                        ImageDefinitionName = config.ImageName,
                        StorageAccountName = storageAccountName,
                        Location = namingContext.Location,
                        Tags = new Dictionary<string, string>(namingContext.Tags),
                        ImageVersionTTLInDays = config.ImageVersionTTLInDays,
                    };
                    _logger.Information("ImageBuilderOptions: {@ImageBuilderOptions}", imgOptions);

                    ImageBuilderOrchestrator orchestrator = new ImageBuilderOrchestrator(
                        azFactory,
                        _timeSource,
                        _logger);

                    if (_options.Action == ActionType.CreateOrUpdateImageGalleryResources)
                    {
                        await orchestrator.CreateOrUpdateInfraAsync(
                            imgOptions,
                            _envOptions.ProvisioningRunnerClientId,
                            _envOptions.AzureVMImageBuilderObjectId,
                            kvName);
                    }
                    else if (_options.Action == ActionType.GenerateCustomizedSBI)
                    {
                        var generatedBuilderTemplate = await orchestrator.BuildCustomizedSBIAsync(
                            imgOptions,
                            _envOptions.ArtifactOptions,
                            _options.ArtifactPath,
                            _options.ImageMetaPath,
                            _envOptions.BaseSBIVerion,
                            cancellationToken);

                        File.WriteAllText("AIB-template.json", generatedBuilderTemplate);
                        _logger.Information("Wrote the generated template in file: AIB-template.json");
                    }
                    else if (_options.Action == ActionType.MoveSBIToOurStorage)
                    {
                        var kvId = $"subscriptions/{azFactory.GenerateLiftrAzure().FluentClient.SubscriptionId}/resourceGroups/{imgOptions.ResourceGroupName}/providers/Microsoft.KeyVault/vaults/{kvName}";

                        await orchestrator.MoveSBIToOurStorageAsync(
                            imgOptions,
                            _envOptions.SBIMoverOptions,
                            kvId,
                            kvClient);
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
