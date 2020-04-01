//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.KeyVault;
using Microsoft.Rest.Azure;
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
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly RunnerCommandOptions _commandOptions;
        private readonly HostingOptions _hostingOptions;

        public ActionExecutor(
            Serilog.ILogger logger,
            IHostApplicationLifetime appLifetime,
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
            try
            {
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

                File.WriteAllText("partner-name.txt", _hostingOptions.PartnerName);
                LogContext.PushProperty("TargetSubscriptionId", hostingEnvironmentOptions.AzureSubscription);
                File.WriteAllText("subscription-id.txt", hostingEnvironmentOptions.AzureSubscription.ToString());
                File.WriteAllText("tenant-id.txt", hostingEnvironmentOptions.TenantId.ToString());
                if (!string.IsNullOrEmpty(_hostingOptions.HelmReleaseName))
                {
                    File.WriteAllText("helm-releasename.txt", _hostingOptions.HelmReleaseName);
                }

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
            return Task.CompletedTask;
        }

        private async Task RunActionAsync(HostingEnvironmentOptions targetOptions, KeyVaultClient kvClient, LiftrAzureFactory azFactory)
        {
            _logger.Information("Target environment options: {@targetOptions}", targetOptions);
            var liftrAzure = azFactory.GenerateLiftrAzure();

            using (var operation = _logger.StartTimedOperation(_commandOptions.Action.ToString()))
            {
                try
                {
                    operation.SetContextProperty(nameof(_hostingOptions.PartnerName), _hostingOptions.PartnerName);
                    operation.SetContextProperty("Environment", _commandOptions.EnvName);
                    operation.SetContextProperty("Location", ToSimpleName(_commandOptions.Region)); // the simple name has a geo coordinate mapping.
                    operation.SetContextProperty("ExeAction", _commandOptions.Action.ToString());

                    var infra = new InfrastructureV2(azFactory, kvClient, _logger);
                    var globalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, targetOptions.Global.Location);
                    var globalRGName = globalNamingContext.ResourceGroupName(targetOptions.Global.BaseName);
                    File.WriteAllText("global-vault-name.txt", globalNamingContext.KeyVaultName(targetOptions.Global.BaseName));

                    if (_commandOptions.Action == ActionType.CreateOrUpdateGlobal)
                    {
                        (var kv, var acr) = await infra.CreateOrUpdateGlobalRGAsync(
                            targetOptions.Global.BaseName,
                            globalNamingContext,
                            targetOptions.DomainName,
                            targetOptions.LogAnalyticsWorkspaceId);
                        File.WriteAllText("acr-name.txt", acr.Name);
                        File.WriteAllText("acr-endpoint.txt", acr.LoginServerUrl);
                        _logger.Information("Successfully managed global resources.");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(targetOptions.LogAnalyticsWorkspaceId))
                        {
                            targetOptions.LogAnalyticsWorkspaceId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourcegroups/{globalRGName}/providers/microsoft.operationalinsights/workspaces/{globalNamingContext.LogAnalyticsName(targetOptions.Global.BaseName)}";
                        }

                        (var regionOptions, var regionalNamingContext) = GetRegionalOptions(targetOptions);
                        var aksRGName = regionalNamingContext.ResourceGroupName(regionOptions.ComputeBaseName);
                        var aksName = regionalNamingContext.AKSName(regionOptions.ComputeBaseName);
                        regionalNamingContext.Tags["GlobalRG"] = globalRGName;

                        if (_commandOptions.Action == ActionType.CreateOrUpdateRegionalData)
                        {
                            regionalNamingContext.Tags["DataRG"] = regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName);

                            CertificateOptions sslCert = null, genevaCert = null, firstPartyCert = null;

                            var hostName = $"{regionalNamingContext.Location.ShortName()}.{targetOptions.DomainName}";
                            sslCert = new CertificateOptions()
                            {
                                CertificateName = "ssl-cert",
                                SubjectName = hostName,
                                SubjectAlternativeNames = new List<string>()
                                    {
                                        hostName,
                                        $"*.{hostName}",
                                        targetOptions.DomainName,
                                        $"*.{targetOptions.DomainName}",
                                    },
                            };

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
                                EnableVNet = targetOptions.EnableVNet,
                                LogAnalyticsWorkspaceId = targetOptions.LogAnalyticsWorkspaceId,
                                DNSZoneId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{globalRGName}/providers/Microsoft.Network/dnszones/{targetOptions.DomainName}",
                            };

                            await infra.CreateOrUpdateRegionalDataRGAsync(regionOptions.DataBaseName, regionalNamingContext, dataOptions);
                            _logger.Information("Successfully managed regional data resources.");
                        }
                        else if (_commandOptions.Action == ActionType.CreateOrUpdateRegionalCompute)
                        {
                            regionalNamingContext.Tags["DataRG"] = regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName);
                            regionalNamingContext.Tags["ComputeRG"] = regionalNamingContext.ResourceGroupName(regionOptions.ComputeBaseName);

                            var acr = await infra.GetACRAsync(targetOptions.Global.BaseName, globalNamingContext);
                            if (acr == null)
                            {
                                var errMsg = "Cannot find the global ACR.";
                                _logger.Fatal(errMsg);
                                throw new InvalidOperationException(errMsg);
                            }

                            File.WriteAllText("acr-name.txt", acr.Name);
                            File.WriteAllText("acr-endpoint.txt", acr.LoginServerUrl);

                            if (string.IsNullOrEmpty(targetOptions.DiagnosticsStorageId))
                            {
                                targetOptions.DiagnosticsStorageId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{globalRGName}/providers/Microsoft.Storage/storageAccounts/{globalNamingContext.StorageAccountName(targetOptions.Global.BaseName)}";
                            }

                            await GetDiagnosticsStorageAccountAsync(azFactory, targetOptions.DiagnosticsStorageId);

                            RegionalComputeOptions regionalComputeOptions = new RegionalComputeOptions()
                            {
                                DataBaseName = regionOptions.DataBaseName,
                                ComputeBaseName = regionOptions.ComputeBaseName,
                                GlobalKeyVaultResourceId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.KeyVault/vaults/{globalNamingContext.KeyVaultName(targetOptions.Global.BaseName)}",
                                LogAnalyticsWorkspaceResourceId = targetOptions.LogAnalyticsWorkspaceId,
                            };

                            (var kv, var msi, var aks) = await infra.CreateOrUpdateRegionalComputeRGAsync(
                                regionalNamingContext,
                                regionalComputeOptions,
                                targetOptions.AKSConfigurations,
                                kvClient,
                                targetOptions.EnableVNet);

                            File.WriteAllText("vault-name.txt", kv.Name);
                            File.WriteAllText("aks-domain.txt", $"{aks.Name}.{targetOptions.DomainName}");
                            File.WriteAllText("aks-name.txt", aks.Name);
                            File.WriteAllText("aks-rg.txt", aks.ResourceGroupName);
                            File.WriteAllText("msi-resourceId.txt", msi.Id);
                            File.WriteAllText("msi-clientId.txt", msi.ClientId);
                            _logger.Information("Successfully managed regional compute resources.");
                        }
                        else if (_commandOptions.Action == ActionType.GetKeyVaultEndpoint)
                        {
                            var kv = await infra.GetKeyVaultAsync(
                                regionOptions.DataBaseName,
                                regionalNamingContext,
                                targetOptions.EnableVNet);

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

                            var hostName = $"{regionalNamingContext.Location.ShortName()}.{targetOptions.DomainName}";
                            File.WriteAllText("rp-hostname.txt", hostName);
                            File.WriteAllText("aks-domain.txt", $"{aksName}.{targetOptions.DomainName}");
                            File.WriteAllText("aks-name.txt", aksName);
                            File.WriteAllText("aks-rg.txt", aksRGName);
                            File.WriteAllText("aks-kv.txt", kv.VaultUri);
                            File.WriteAllText("vault-name.txt", kv.Name);

                            if (string.IsNullOrEmpty(targetOptions.DiagnosticsStorageId))
                            {
                                targetOptions.DiagnosticsStorageId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{globalRGName}/providers/Microsoft.Storage/storageAccounts/{globalNamingContext.StorageAccountName(targetOptions.Global.BaseName)}";
                            }

                            await GetDiagnosticsStorageAccountAsync(azFactory, targetOptions.DiagnosticsStorageId);

                            _logger.Information("Successfully retrieved Key Vault endpoint.");
                        }
                        else if (_commandOptions.Action == ActionType.UpdateAKSPublicIpInTrafficManager)
                        {
                            var az = liftrAzure.FluentClient;
                            var aksHelper = new AKSHelper(_logger);
                            var tmId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName)}/providers/Microsoft.Network/trafficmanagerprofiles/{regionalNamingContext.TrafficManagerName(regionOptions.DataBaseName)}";

                            var dnsZone = await liftrAzure.GetDNSZoneAsync(globalRGName, targetOptions.DomainName);
                            if (dnsZone == null)
                            {
                                var errMsg = $"Cannot find the DNS zone for domina '{targetOptions.DomainName}' in RG '{globalRGName}'.";
                                _logger.Error(errMsg);
                                throw new InvalidOperationException(errMsg);
                            }

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

                            await dnsZone.Update().DefineARecordSet(aksName).WithIPv4Address(pip.IPAddress).WithTimeToLive(60).Attach().ApplyAsync();
                            await dnsZone.Update().DefineARecordSet("*." + aksName).WithIPv4Address(pip.IPAddress).WithTimeToLive(60).Attach().ApplyAsync();
                            await dnsZone.Update().DefineARecordSet("thanos-0-" + aksName).WithIPv4Address(pip.IPAddress).WithTimeToLive(60).Attach().ApplyAsync();
                            await dnsZone.Update().DefineARecordSet("thanos-1-" + aksName).WithIPv4Address(pip.IPAddress).WithTimeToLive(60).Attach().ApplyAsync();
                            _logger.Information("Successfully DNS A record '{recordName}' to IP '{ipAddress}'.", aksName, pip.IPAddress);
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

        private async Task GetDiagnosticsStorageAccountAsync(LiftrAzureFactory azFactory, string diagnosticsStorageId)
        {
            IStorageAccount diagStor = null;
            var rid = new Liftr.Contracts.ResourceId(diagnosticsStorageId);
            diagStor = await azFactory.GenerateLiftrAzure(rid.SubscriptionId).FluentClient.StorageAccounts.GetByIdAsync(diagnosticsStorageId);

            if (diagStor == null)
            {
                var errMsg = "Cannot find the global diagnostics storage account.";
                _logger.Fatal(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            var storKey = await diagStor.GetPrimaryStorageKeyAsync();

            File.WriteAllText("diag-stor-name.txt", diagStor.Name);
            File.WriteAllText("diag-stor-key.txt", storKey.Value);
        }

        private static string ToSimpleName(string region)
        {
            return region.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
        }
    }
}
