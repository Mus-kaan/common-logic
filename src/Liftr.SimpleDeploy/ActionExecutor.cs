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
        private readonly RunnerCommandOptions _commandOptions;
        private readonly HostingOptions _hostingOptions;

        public ActionExecutor(
            Serilog.ILogger logger,
            IApplicationLifetime appLifetime,
            RunnerCommandOptions runnerOptions,
            HostingOptions hostingOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
            _commandOptions = runnerOptions ?? throw new ArgumentNullException(nameof(runnerOptions));
            _hostingOptions = hostingOptions ?? throw new ArgumentNullException(nameof(hostingOptions));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Start ActionExecutor ...");

            try
            {
                LogContext.PushProperty(nameof(_commandOptions.EnvName), _commandOptions.EnvName);
                LogContext.PushProperty(nameof(_commandOptions.Region), _commandOptions.Region);
                LogContext.PushProperty("ExeAction", _commandOptions.Action);

                if (!File.Exists(_commandOptions.ConfigPath))
                {
                    var errMsg = $"Config json file doesn't exist at the path: {_commandOptions.ConfigPath}";
                    _logger.Fatal(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                var hostingEnvironmentOptions = _hostingOptions.Environments.Where(env => env.EnvironmentName.ToString().OrdinalEquals(_commandOptions.EnvName)).FirstOrDefault();

                if (hostingEnvironmentOptions == null)
                {
                    var ex = new InvalidOperationException($"Cannot find the hosting environment with name '{_commandOptions.EnvName}' in the configuration file: {_commandOptions.ConfigPath}");
                    _logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                LogContext.PushProperty("TargetSubscriptionId", hostingEnvironmentOptions.AzureSubscription);
                File.WriteAllText("subscription-id.txt", hostingEnvironmentOptions.AzureSubscription.ToString());

                _logger.Information("ActionExecutor Action:{ExeAction}", _commandOptions.Action);
                _logger.Information("RunnerCommandOptions: {@RunnerCommandOptions}", _commandOptions);

                if (_commandOptions.Action == ActionType.OutputSubscriptionId)
                {
                    _appLifetime.StopApplication();
                    return Task.CompletedTask;
                }

                TokenCredential tokenCredential = null;
                Func<AzureCredentials> azureCredentialsProvider = null;
                KeyVaultClient kvClient = null;
                if (!string.IsNullOrEmpty(_commandOptions.AuthFile))
                {
                    _logger.Information("Use auth json file to authenticate against Azure.");
                    var authContract = AuthFileContract.FromFile(_commandOptions.AuthFile);
                    kvClient = KeyVaultClientFactory.FromClientIdAndSecret(authContract.ClientId, authContract.ClientSecret);

                    if (!string.IsNullOrEmpty(authContract.ServicePrincipalObjectId))
                    {
                        _commandOptions.ExecutingSPNObjectId = authContract.ServicePrincipalObjectId;
                    }

                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory.FromFile(_commandOptions.AuthFile);
                    tokenCredential = new ClientSecretCredential(authContract.TenantId, authContract.ClientId, authContract.ClientSecret);
                }
                else
                {
                    _logger.Information("Use MSI to authenticate against Azure.");
                    kvClient = KeyVaultClientFactory.FromMSI();

                    azureCredentialsProvider = () => SdkContext.AzureCredentialsFactory
                    .FromMSI(new MSILoginInformation(MSIResourceType.VirtualMachine), AzureEnvironment.AzureGlobalCloud, hostingEnvironmentOptions.TenantId.ToString())
                    .WithDefaultSubscription(hostingEnvironmentOptions.AzureSubscription.ToString());
                    tokenCredential = new ManagedIdentityCredential();
                }

                if (string.IsNullOrEmpty(_commandOptions.ExecutingSPNObjectId))
                {
                    var ex = new InvalidOperationException("Must provide an object Id of the executing spn.");
                    _logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                if (_commandOptions.Action == ActionType.UpdateAKSPublicIpInTrafficManager)
                {
                    if (string.IsNullOrEmpty(_commandOptions.AKSAppSvcLabel))
                    {
                        var ex = new InvalidOperationException("Must provide an AKS svc label to get the IP address.");
                        _logger.Fatal(ex, ex.Message);
                        throw ex;
                    }
                }

                _hostingOptions.CheckValid();

                LiftrAzureFactory azFactory = new LiftrAzureFactory(
                    _logger,
                    hostingEnvironmentOptions.TenantId.ToString(),
                    _commandOptions.ExecutingSPNObjectId,
                    hostingEnvironmentOptions.AzureSubscription.ToString(),
                    tokenCredential,
                    azureCredentialsProvider);

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

            using (var operation = _logger.StartTimedOperation(_commandOptions.Action.ToString()))
            {
                try
                {
                    operation.SetContextProperty(nameof(_hostingOptions.PartnerName), _hostingOptions.PartnerName);

                    var infra = new InftrastructureV2(azFactory, _logger);

                    var globalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, targetOptions.Global.Location);

                    if (_commandOptions.Action == ActionType.CreateOrUpdateGlobal)
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

                        if (_commandOptions.Action == ActionType.CreateOrUpdateRegionalData)
                        {
                            CertificateOptions sslCert = null, genevaCert = null, firstPartyCert = null;
                            if (!string.IsNullOrEmpty(regionOptions.HostName))
                            {
                                sslCert = new CertificateOptions()
                                {
                                    CertificateName = "ssl-cert",
                                    SubjectName = regionOptions.HostName,
                                    SubjectAlternativeNames = new List<string>() { regionOptions.HostName },
                                };
                            }

                            genevaCert = new CertificateOptions()
                            {
                                CertificateName = "GenevaClientCert",
                                SubjectName = targetOptions.GenevaCertificateSubjectName,
                                SubjectAlternativeNames = new List<string>() { targetOptions.GenevaCertificateSubjectName },
                            };

                            if (!string.IsNullOrEmpty(targetOptions.FirstPartyAppCertificateSubjectName))
                            {
                                firstPartyCert = new CertificateOptions()
                                {
                                    CertificateName = "FirstPartyAppCert",
                                    SubjectName = targetOptions.FirstPartyAppCertificateSubjectName,
                                    SubjectAlternativeNames = new List<string>() { targetOptions.FirstPartyAppCertificateSubjectName },
                                };
                            }

                            var dataOptions = new RegionalDataOptions()
                            {
                                ActiveDBKeyName = _commandOptions.ActiveKeyName,
                                SecretPrefix = _hostingOptions.SecretPrefix,
                                GenevaCert = genevaCert,
                                SSLCert = sslCert,
                                FirstPartyCert = firstPartyCert,
                                DataPlaneSubscriptions = regionOptions.DataPlaneSubscriptions,
                                DataPlaneStorageCountPerSubscription = _hostingOptions.StorageCountPerDataPlaneSubscription,
                            };

                            await infra.CreateOrUpdateRegionalDataRGAsync(regionOptions.DataBaseName, regionalNamingContext, dataOptions, kvClient);
                            _logger.Information("Successfully managed regional data resources.");
                        }
                        else if (_commandOptions.Action == ActionType.CreateOrUpdateRegionalCompute)
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
                                targetOptions.AKSConfigurations,
                                kvClient);

                            File.WriteAllText("vault-name.txt", kv.Name);
                            File.WriteAllText("aks-name.txt", aks.Name);
                            File.WriteAllText("aks-rg.txt", aks.ResourceGroupName);
                            File.WriteAllText("msi-resourceId.txt", msi.Id);
                            File.WriteAllText("msi-clientId.txt", msi.ClientId);
                            _logger.Information("Successfully managed regional compute resources.");
                        }
                        else if (_commandOptions.Action == ActionType.GetKeyVaultEndpoint)
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
                        else if (_commandOptions.Action == ActionType.UpdateAKSPublicIpInTrafficManager)
                        {
                            var az = azFactory.GenerateLiftrAzure().FluentClient;
                            var aksHelper = new AKSHelper(_logger);
                            var tmId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName)}/providers/Microsoft.Network/trafficmanagerprofiles/{regionalNamingContext.TrafficManagerName(regionOptions.DataBaseName)}";

                            var pip = await aksHelper.GetAppPublicIpAsync(az, aksRGName, aksName, regionalNamingContext.Location, _commandOptions.AKSAppSvcLabel);
                            if (pip == null)
                            {
                                var errMsg = $"Cannot find the public Ip address for the AKS cluster. aksRGName:{aksRGName}, aksName:{aksName}, region:{regionalNamingContext.Location}, aksSvcLabel:{_commandOptions.AKSAppSvcLabel}.";
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
                                _commandOptions,
                                _hostingOptions,
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
            var location = Region.Create(_commandOptions.Region);
            var regionOptions = targetOptions.Regions.Where(r => r.Location.Name.OrdinalEquals(location.Name)).FirstOrDefault();
            if (regionOptions == null)
            {
                var ex = new InvalidOperationException($"Cannot find the '{_commandOptions.Region}' region configurations.");
                _logger.Fatal(ex, ex.Message);
                throw ex;
            }

            var regionalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, regionOptions.Location);

            return (regionOptions, regionalNamingContext);
        }
    }
}
