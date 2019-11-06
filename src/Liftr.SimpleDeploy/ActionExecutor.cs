//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.KeyVault;
using Microsoft.Liftr.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.SimpleDeploy
{
    public sealed class ActionExecutor : IHostedService
    {
        private readonly Serilog.ILogger _logger;
        private readonly IApplicationLifetime _appLifetime;
        private readonly RunnerCommandOptions _options;
        private readonly EnvironmentOptions _envOptions;

        public ActionExecutor(
            Serilog.ILogger logger,
            IApplicationLifetime appLifetime,
            RunnerCommandOptions runnerOptions,
            IOptions<EnvironmentOptions> envOptions)
        {
            if (envOptions == null)
            {
                throw new ArgumentNullException(nameof(envOptions));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
            _options = runnerOptions ?? throw new ArgumentNullException(nameof(runnerOptions));
            _envOptions = envOptions.Value;

            LogContext.PushProperty("TargetSubscriptionId", runnerOptions.SubscriptionId);
            LogContext.PushProperty("ExeAction", runnerOptions.Action);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Start ActionExecutor ...");

            try
            {
                if (_envOptions.GenevaCert == null)
                {
                    throw new InvalidOperationException($"{nameof(_envOptions.GenevaCert)} is null.");
                }

                if (string.IsNullOrEmpty(_options.SubscriptionId))
                {
                    throw new InvalidOperationException("Please sepcify a valid Subscription Id.");
                }

                LogContext.PushProperty("TargetSubscriptionId", _options.SubscriptionId);
                LogContext.PushProperty("ExeAction", _options.Action);

                if (!File.Exists(_options.ConfigPath))
                {
                    var errMsg = $"Config json file doesn't exist at the path: {_options.ConfigPath}";
                    _logger.Fatal(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                _logger.Information("ActionExecutor Action:{ExeAction}", _options.Action);
                _logger.Information("RunnerCommandOptions: {@RunnerCommandOptions}", _options);

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

                if (_options.Action == ActionType.UpdateAKSPublicIpInTrafficManager)
                {
                    if (string.IsNullOrEmpty(_options.AKSAppSvcLabel))
                    {
                        throw new InvalidOperationException("Must provide an AKS svc label to get the IP address.");
                    }
                }

                LiftrAzureFactory azFactory = new LiftrAzureFactory(_logger, _envOptions.TenantId, _envOptions.ProvisioningRunnerClientId, _options.SubscriptionId, azureCredentialsProvider);

                _ = RunActionAsync(kvClient, azFactory);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "ActionExecutor Failed.");
                throw;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Stop ActionExecutor ...");
            return Task.CompletedTask;
        }

        private async Task RunActionAsync(KeyVaultClient kvClient, LiftrAzureFactory azFactory)
        {
            using (var operation = _logger.StartTimedOperation(_options.Action.ToString()))
            {
                try
                {
                    var content = File.ReadAllText(_options.ConfigPath);
                    BaseResourceOptions config = null;
                    GlobalResourceOptions gblOptions = null;
                    DataResourceOptions dataOptions = null;
                    ComputeResourceOptions computeOptions = null;

                    switch (_options.Action)
                    {
                        case ActionType.CreateOrUpdateGlobal:
                            {
                                gblOptions = content.FromJson<GlobalResourceOptions>();
                                operation.SetContextProperty(nameof(gblOptions.GlobalBaseName), gblOptions.GlobalBaseName);
                                config = gblOptions;
                                break;
                            }

                        case ActionType.CreateOrUpdateRegionalData:
                            {
                                dataOptions = content.FromJson<DataResourceOptions>();
                                operation.SetContextProperty(nameof(dataOptions.DataBaseName), dataOptions.DataBaseName);
                                config = dataOptions;
                                break;
                            }

                        case ActionType.CreateOrUpdateRegionalCompute:
                        case ActionType.GetComputeKeyVaultEndpoint:
                        case ActionType.UpdateAKSPublicIpInTrafficManager:
                            {
                                computeOptions = content.FromJson<ComputeResourceOptions>();
                                if (!string.IsNullOrEmpty(computeOptions.GlobalBaseName))
                                {
                                    operation.SetContextProperty(nameof(computeOptions.GlobalBaseName), computeOptions.GlobalBaseName);
                                }

                                if (!string.IsNullOrEmpty(computeOptions.DataBaseName))
                                {
                                    operation.SetContextProperty(nameof(computeOptions.DataBaseName), computeOptions.DataBaseName);
                                }

                                operation.SetContextProperty(nameof(computeOptions.ComputeBaseName), computeOptions.ComputeBaseName);

                                config = computeOptions;
                                break;
                            }

                        default:
                            {
                                throw new InvalidOperationException("Unsupported action: " + _options.Action);
                            }
                    }

                    _logger.Information("Parsed config file: {config}", config);

                    operation.SetContextProperty(nameof(config.PartnerName), config.PartnerName);
                    operation.SetContextProperty(nameof(config.Environment), config.Environment.ToString());
                    if (_options.Action == ActionType.CreateOrUpdateGlobal)
                    {
                        operation.SetContextProperty("DeploymentLocation", "global");
                    }
                    else
                    {
                        operation.SetContextProperty("DeploymentLocation", config.LocationStr);
                    }

                    var infra = new InftrastructureV2(azFactory, _logger);
                    var namingContext = new NamingContext(config.PartnerName, config.ShortPartnerName, config.Environment, config.Location);

                    if (_options.Action == ActionType.CreateOrUpdateGlobal)
                    {
                        await infra.CreateOrUpdateGlobalRGAsync(gblOptions.GlobalBaseName, namingContext);
                        _logger.Information("Successfully managed global resources.");
                    }
                    else if (_options.Action == ActionType.CreateOrUpdateRegionalData)
                    {
                        await infra.CreateOrUpdateRegionalDataRGAsync(dataOptions.DataBaseName, namingContext, dataOptions.CreateRegionalKeyVault, dataOptions.DataPlaneStorageCount);
                        _logger.Information("Successfully managed regional data resources.");
                    }
                    else if (_options.Action == ActionType.CreateOrUpdateRegionalCompute)
                    {
                        InfraV2RegionalComputeOptions v2Options = new InfraV2RegionalComputeOptions()
                        {
                            DataBaseName = computeOptions.DataBaseName,
                            ComputeBaseName = computeOptions.ComputeBaseName,
                            SecretPrefix = computeOptions.SecretPrefix,
                            CopyKVSecretsWithPrefix = namingContext.PartnerName,
                            DataPlaneSubscriptions = computeOptions.DataPlaneSubscriptions,
                        };

                        if (!string.IsNullOrEmpty(computeOptions.GlobalBaseName))
                        {
                            var gblNamingContext = new NamingContext(namingContext.PartnerName, namingContext.ShortPartnerName, namingContext.Environment, computeOptions.GlobalLocation);
                            v2Options.CentralKeyVaultResourceId = $"subscriptions/{_options.SubscriptionId}/resourceGroups/{gblNamingContext.ResourceGroupName(computeOptions.GlobalBaseName)}/providers/Microsoft.KeyVault/vaults/{gblNamingContext.KeyVaultName(computeOptions.GlobalBaseName)}";
                        }

                        var sslCert = new CertificateOptions()
                        {
                            CertificateName = "ssl-cert",
                            SubjectName = computeOptions.HostName,
                            SubjectAlternativeNames = new List<string>() { computeOptions.HostName },
                        };

                        (var kv, var msi, var aks) = await infra.CreateOrUpdateRegionalComputeRGAsync(
                            namingContext,
                            v2Options,
                            _envOptions.AKSInfo,
                            kvClient,
                            genevaCert: _envOptions.GenevaCert,
                            sslCert: sslCert,
                            firstPartyCert: _envOptions.FirstPartyCert);

                        File.WriteAllText("vault-name.txt", kv.Name);
                        File.WriteAllText("aks-name.txt", aks.Name);
                        File.WriteAllText("aks-rg.txt", aks.ResourceGroupName);
                        File.WriteAllText("msi-resourceId.txt", msi.Id);
                        File.WriteAllText("msi-clientId.txt", msi.ClientId);
                        _logger.Information("Successfully managed regional compute resources.");
                    }
                    else if (_options.Action == ActionType.GetComputeKeyVaultEndpoint)
                    {
                        var aksRGName = namingContext.ResourceGroupName(computeOptions.ComputeBaseName);
                        var aksName = namingContext.AKSName(computeOptions.ComputeBaseName);
                        var kv = await infra.GetKeyVaultAsync(computeOptions.ComputeBaseName, namingContext);
                        if (kv == null)
                        {
                            var errMsg = "Cannot find key vault in the compute rg";
                            _logger.Fatal(errMsg);
                            throw new InvalidOperationException(errMsg);
                        }

                        File.WriteAllText("rp-hostname.txt", computeOptions.HostName);
                        File.WriteAllText("aks-name.txt", aksName);
                        File.WriteAllText("aks-rg.txt", aksRGName);
                        File.WriteAllText("aks-kv.txt", kv.VaultUri);
                        File.WriteAllText("vault-name.txt", kv.Name);
                        _logger.Information("Successfully retrieved Key Vault endpoint.");
                    }
                    else if (_options.Action == ActionType.UpdateAKSPublicIpInTrafficManager)
                    {
                        var az = azFactory.GenerateLiftrAzure().FluentClient;
                        var aksHelper = new AKSHelper(_logger);
                        var aksRGName = namingContext.ResourceGroupName(computeOptions.ComputeBaseName);
                        var aksName = namingContext.AKSName(computeOptions.ComputeBaseName);
                        var tmId = $"subscriptions/{_options.SubscriptionId}/resourceGroups/{namingContext.ResourceGroupName(computeOptions.DataBaseName)}/providers/Microsoft.Network/trafficmanagerprofiles/{namingContext.TrafficManagerName(computeOptions.DataBaseName)}";

                        var pip = await aksHelper.GetAppPublicIpAsync(az, aksRGName, aksName, namingContext.Location, _options.AKSAppSvcLabel);
                        if (pip == null)
                        {
                            var errMsg = $"Cannot find the public Ip address for the AKS cluster. aksRGName:{aksRGName}, aksName:{aksName}, region:{namingContext.Location}, aksSvcLabel:{_options.AKSAppSvcLabel}.";
                            _logger.Error(errMsg);
                            throw new InvalidOperationException(errMsg);
                        }

                        _logger.Information("Find the IP of the AKS is: {IPAddress}", pip.IPAddress);

                        if (string.IsNullOrEmpty(pip.IPAddress))
                        {
                            _logger.Error("The IP address is null of the created Pulic IP with Id {PipResourceId}", pip.Id);
                            throw new InvalidOperationException($"The IP address is null of the created Pulic IP with Id {pip.Id}");
                        }

                        var epName = $"{aksRGName}-{SdkContext.RandomResourceName(string.Empty, 5).Substring(0, 3)}";
                        _logger.Information("New endpoint name: {epName}", epName);
                        await aksHelper.AddPulicIpToTrafficManagerAsync(az, tmId, epName, pip.IPAddress, enabled: true);

                        _logger.Information("Successfully updated AKS public IP in the traffic manager.");
                    }

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
