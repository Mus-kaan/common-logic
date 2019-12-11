//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.KeyVault;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                LogContext.PushProperty(nameof(_options.EnvName), _options.EnvName);
                LogContext.PushProperty(nameof(_options.Region), _options.Region);
                LogContext.PushProperty("ExeAction", _options.Action);

                if (!File.Exists(_options.ConfigPath))
                {
                    var errMsg = $"Config json file doesn't exist at the path: {_options.ConfigPath}";
                    _logger.Fatal(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                var hostingOptions = File.ReadAllText(_options.ConfigPath).FromJson<HostingOptions>();
                var hostingEnvironmentOptions = hostingOptions.Environments.Where(env => env.EnvironmentName.ToString().OrdinalEquals(_options.EnvName)).FirstOrDefault();

                if (hostingEnvironmentOptions == null)
                {
                    var ex = new InvalidOperationException($"Cannot find the hosting environment with name '{_options.EnvName}' in the configuration file: {_options.ConfigPath}");
                    _logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                LogContext.PushProperty("TargetSubscriptionId", hostingEnvironmentOptions.AzureSubscription);

                _logger.Information("ActionExecutor Action:{ExeAction}", _options.Action);
                _logger.Information("RunnerCommandOptions: {@RunnerCommandOptions}", _options);

                TokenCredential tokenCredential = null;
                Func<AzureCredentials> azureCredentialsProvider = null;
                KeyVaultClient kvClient = null;
                if (!string.IsNullOrEmpty(_options.AuthFile))
                {
                    _logger.Information("Use auth json file to authenticate against Azure.");
                    var authContract = AuthFileContract.FromFile(_options.AuthFile);
                    kvClient = KeyVaultClientFactory.FromClientIdAndSecret(authContract.ClientId, authContract.ClientSecret);

                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory.FromFile(_options.AuthFile);
                    tokenCredential = new ClientSecretCredential(authContract.TenantId, authContract.ClientId, authContract.ClientSecret);
                }
                else
                {
                    _logger.Information("Use MSI to authenticate against Azure.");
                    kvClient = KeyVaultClientFactory.FromMSI();

                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory
                    .FromMSI(new MSILoginInformation(MSIResourceType.VirtualMachine), AzureEnvironment.AzureGlobalCloud, _envOptions.TenantId)
                    .WithDefaultSubscription(hostingEnvironmentOptions.AzureSubscription.ToString());
                    tokenCredential = new ManagedIdentityCredential();
                }

                if (_options.Action == ActionType.UpdateAKSPublicIpInTrafficManager)
                {
                    if (string.IsNullOrEmpty(_options.AKSAppSvcLabel))
                    {
                        var ex = new InvalidOperationException("Must provide an AKS svc label to get the IP address.");
                        _logger.Fatal(ex, ex.Message);
                        throw ex;
                    }
                }

                LiftrAzureFactory azFactory = new LiftrAzureFactory(
                    _logger,
                    _envOptions.TenantId,
                    _envOptions.SPNObjectId,
                    hostingEnvironmentOptions.AzureSubscription.ToString(),
                    tokenCredential,
                    azureCredentialsProvider,
                    _envOptions.LiftrAzureOptions);

                _ = RunActionAsync(hostingEnvironmentOptions, kvClient, azFactory);
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

        private async Task RunActionAsync(HostingEnvironmentOptions targetOptions, KeyVaultClient kvClient, LiftrAzureFactory azFactory)
        {
            _logger.Information("Target environment options: {@targetOptions}", targetOptions);

            using (var operation = _logger.StartTimedOperation(_options.Action.ToString()))
            {
                try
                {
                    operation.SetContextProperty(nameof(_envOptions.PartnerName), _envOptions.PartnerName);

                    var infra = new InftrastructureV2(azFactory, _logger);

                    var globalNamingContext = new NamingContext(_envOptions.PartnerName, _envOptions.ShortPartnerName, targetOptions.EnvironmentName, targetOptions.Global.Location);

                    File.WriteAllText("subscription-id.txt", targetOptions.AzureSubscription.ToString());

                    if (_options.Action == ActionType.CreateOrUpdateGlobal)
                    {
                        (var kv, var acr) = await infra.CreateOrUpdateGlobalRGAsync(targetOptions.Global.BaseName, globalNamingContext);
                        File.WriteAllText("acr-name.txt", acr.Name);
                        File.WriteAllText("acr-endpoint.txt", acr.LoginServerUrl);
                        _logger.Information("Successfully managed global resources.");
                    }
                    else
                    {
                        (var regionOptions, var regionalNamingContext) = GetRegionalOptions(targetOptions);
                        var aksRGName = regionalNamingContext.ResourceGroupName(regionOptions.ComputeBaseName);
                        var aksName = regionalNamingContext.AKSName(regionOptions.ComputeBaseName);

                        if (_options.Action == ActionType.CreateOrUpdateRegionalData)
                        {
                            CertificateOptions sslCert = null;
                            if (!string.IsNullOrEmpty(regionOptions.HostName))
                            {
                                sslCert = new CertificateOptions()
                                {
                                    CertificateName = "ssl-cert",
                                    SubjectName = regionOptions.HostName,
                                    SubjectAlternativeNames = new List<string>() { regionOptions.HostName },
                                };
                            }

                            var dataOptions = new RegionalDataOptions()
                            {
                                ActiveDBKeyName = _options.ActiveKeyName,
                                SecretPrefix = _envOptions.SecretPrefix,
                                GenevaCert = _envOptions.GenevaCert,
                                SSLCert = sslCert,
                                FirstPartyCert = _envOptions.FirstPartyCert,
                                DataPlaneSubscriptions = regionOptions.DataPlaneSubscriptions,
                                DataPlaneStorageCountPerSubscription = _envOptions.StorageCountPerDataPlaneSubscription,
                            };

                            await infra.CreateOrUpdateRegionalDataRGAsync(regionOptions.DataBaseName, regionalNamingContext, dataOptions, kvClient);
                            _logger.Information("Successfully managed regional data resources.");
                        }
                        else if (_options.Action == ActionType.CreateOrUpdateRegionalCompute)
                        {
                            var acr = await infra.GetACRAsync(targetOptions.Global.BaseName, globalNamingContext);
                            if (acr == null)
                            {
                                var errMsg = "Cannot find the global ACR.";
                                _logger.Fatal(errMsg);
                                throw new InvalidOperationException(errMsg);
                            }

                            File.WriteAllText("acr-name.txt", acr.Name);
                            File.WriteAllText("acr-endpoint.txt", acr.LoginServerUrl);

                            RegionalComputeOptions regionalComputeOptions = new RegionalComputeOptions()
                            {
                                DataBaseName = regionOptions.DataBaseName,
                                ComputeBaseName = regionOptions.ComputeBaseName,
                                GlobalKeyVaultResourceId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalNamingContext.ResourceGroupName(targetOptions.Global.BaseName)}/providers/Microsoft.KeyVault/vaults/{globalNamingContext.KeyVaultName(targetOptions.Global.BaseName)}",
                            };

                            (var kv, var msi, var aks) = await infra.CreateOrUpdateRegionalComputeRGAsync(
                                regionalNamingContext,
                                regionalComputeOptions,
                                _envOptions.AKSInfo,
                                kvClient);

                            File.WriteAllText("vault-name.txt", kv.Name);
                            File.WriteAllText("aks-name.txt", aks.Name);
                            File.WriteAllText("aks-rg.txt", aks.ResourceGroupName);
                            File.WriteAllText("msi-resourceId.txt", msi.Id);
                            File.WriteAllText("msi-clientId.txt", msi.ClientId);
                            _logger.Information("Successfully managed regional compute resources.");
                        }
                        else if (_options.Action == ActionType.GetKeyVaultEndpoint)
                        {
                            var kv = await infra.GetKeyVaultAsync(regionOptions.DataBaseName, regionalNamingContext);
                            if (kv == null)
                            {
                                var errMsg = "Cannot find key vault in the regional data resource group.";
                                _logger.Fatal(errMsg);
                                throw new InvalidOperationException(errMsg);
                            }

                            var acr = await infra.GetACRAsync(targetOptions.Global.BaseName, globalNamingContext);
                            if (acr == null)
                            {
                                var errMsg = "Cannot find the global ACR.";
                                _logger.Fatal(errMsg);
                                throw new InvalidOperationException(errMsg);
                            }

                            File.WriteAllText("acr-name.txt", acr.Name);
                            File.WriteAllText("acr-endpoint.txt", acr.LoginServerUrl);

                            if (!string.IsNullOrEmpty(regionOptions.HostName))
                            {
                                File.WriteAllText("rp-hostname.txt", regionOptions.HostName);
                            }

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
                            var tmId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName)}/providers/Microsoft.Network/trafficmanagerprofiles/{regionalNamingContext.TrafficManagerName(regionOptions.DataBaseName)}";

                            var pip = await aksHelper.GetAppPublicIpAsync(az, aksRGName, aksName, regionalNamingContext.Location, _options.AKSAppSvcLabel);
                            if (pip == null)
                            {
                                var errMsg = $"Cannot find the public Ip address for the AKS cluster. aksRGName:{aksRGName}, aksName:{aksName}, region:{regionalNamingContext.Location}, aksSvcLabel:{_options.AKSAppSvcLabel}.";
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
                    }

                    if (SimpleDeployExtension.AfterRunAsync != null)
                    {
                        using (_logger.StartTimedOperation("Run extension action"))
                        {
                            var extensionTask = SimpleDeployExtension.AfterRunAsync.Invoke(
                                azFactory,
                                kvClient,
                                _options,
                                _envOptions,
                                _logger);
                            await extensionTask;
                        }
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

        private (RegionOptions, NamingContext) GetRegionalOptions(HostingEnvironmentOptions targetOptions)
        {
            var location = Region.Create(_options.Region);
            var regionOptions = targetOptions.Regions.Where(r => r.Location.Name.OrdinalEquals(location.Name)).FirstOrDefault();
            if (regionOptions == null)
            {
                var ex = new InvalidOperationException($"Cannot find the '{_options.Region}' region configurations.");
                _logger.Fatal(ex, ex.Message);
                throw ex;
            }

            var regionalNamingContext = new NamingContext(_envOptions.PartnerName, _envOptions.ShortPartnerName, targetOptions.EnvironmentName, regionOptions.Location);

            return (regionOptions, regionalNamingContext);
        }
    }
}
