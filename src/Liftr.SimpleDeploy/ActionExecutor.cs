//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.KeyVault;
using Microsoft.Rest.Azure;
using Serilog.Context;
using System;
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

            var callBackConfigs = new SimpleDeployConfigurations()
            {
                LiftrAzureFactory = azFactory,
                KeyVaultClient = kvClient,
                RunnerCommandOptions = _commandOptions,
                HostingOptions = _hostingOptions,
                Logger = _logger,
            };

            using (var operation = _logger.StartTimedOperation(_commandOptions.Action.ToString()))
            {
                try
                {
                    operation.SetContextProperty(nameof(_hostingOptions.PartnerName), _hostingOptions.PartnerName);
                    operation.SetContextProperty("Environment", _commandOptions.EnvName);
                    operation.SetContextProperty("Location", ToSimpleName(_commandOptions.Region)); // the simple name has a geo coordinate mapping.
                    operation.SetContextProperty("ExeAction", _commandOptions.Action.ToString());

                    var infra = new InfrastructureV2(azFactory, kvClient, _logger);
                    IPPoolManager ipPool = null;
                    var globalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, targetOptions.Global.Location);
                    var globalRGName = globalNamingContext.ResourceGroupName(targetOptions.Global.BaseName);
                    File.WriteAllText("global-vault-name.txt", globalNamingContext.KeyVaultName(targetOptions.Global.BaseName));

                    _logger.Information("Current correlation Id is: {correlationId}", TelemetryContext.GetOrGenerateCorrelationId());
                    _logger.Information("You can use correlation Id '{correlationId}' to query all the related ARM logs.", TelemetryContext.GetOrGenerateCorrelationId());

                    if (targetOptions.IPPerRegion > 0)
                    {
                        var ipNamePrefix = globalNamingContext.GenerateCommonName(targetOptions.Global.BaseName, noRegion: true);
                        var poolRG = ipNamePrefix + "-ip-pool-rg";
                        ipPool = new IPPoolManager(poolRG, ipNamePrefix, azFactory, _logger);
                    }

                    if (_commandOptions.Action == ActionType.CreateOrUpdateGlobal)
                    {
                        if (targetOptions.IPPerRegion > 0)
                        {
                            var regions = targetOptions.Regions.Select(r => r.Location);
                            await ipPool.ProvisionIPPoolAsync(targetOptions.Global.Location, targetOptions.IPPerRegion, regions, globalNamingContext.Tags);
                        }

                        var globalResources = await infra.CreateOrUpdateGlobalRGAsync(
                            targetOptions.Global.BaseName,
                            globalNamingContext,
                            targetOptions.DomainName,
                            targetOptions.LogAnalyticsWorkspaceId);

                        File.WriteAllText("acr-name.txt", globalResources.ContainerRegistry.Name);
                        File.WriteAllText("acr-endpoint.txt", globalResources.ContainerRegistry.LoginServerUrl);

                        if (SimpleDeployExtension.AfterProvisionGlobalResourcesAsync != null)
                        {
                            using (_logger.StartTimedOperation(nameof(SimpleDeployExtension.AfterProvisionGlobalResourcesAsync)))
                            {
                                var parameters = new GlobalCallbackParameters()
                                {
                                    CallbackConfigurations = callBackConfigs,
                                    BaseName = targetOptions.Global.BaseName,
                                    NamingContext = globalNamingContext,
                                    Resources = globalResources,
                                };

                                await SimpleDeployExtension.AfterProvisionGlobalResourcesAsync.Invoke(parameters);
                            }
                        }

                        _logger.Information("-----------------------------------------------------------------------");
                        _logger.Information($"Successfully finished managing global resources.");
                        _logger.Information("-----------------------------------------------------------------------");
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

                            var dataOptions = new RegionalDataOptions()
                            {
                                ActiveDBKeyName = _commandOptions.ActiveKeyName,
                                SecretPrefix = _hostingOptions.SecretPrefix,
                                OneCertCertificates = targetOptions.OneCertCertificates,
                                DataPlaneSubscriptions = regionOptions.DataPlaneSubscriptions,
                                DataPlaneStorageCountPerSubscription = _hostingOptions.StorageCountPerDataPlaneSubscription,
                                EnableVNet = targetOptions.EnableVNet,
                                GlobalKeyVaultResourceId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.KeyVault/vaults/{globalNamingContext.KeyVaultName(targetOptions.Global.BaseName)}",
                                LogAnalyticsWorkspaceId = targetOptions.LogAnalyticsWorkspaceId,
                                DomainName = targetOptions.DomainName,
                                DNSZoneId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{globalRGName}/providers/Microsoft.Network/dnszones/{targetOptions.DomainName}",
                            };

                            var dataResources = await infra.CreateOrUpdateRegionalDataRGAsync(regionOptions.DataBaseName, regionalNamingContext, dataOptions);

                            if (SimpleDeployExtension.AfterProvisionRegionalDataResourcesAsync != null)
                            {
                                using (_logger.StartTimedOperation(nameof(SimpleDeployExtension.AfterProvisionRegionalDataResourcesAsync)))
                                {
                                    var parameters = new RegionalDataCallbackParameters()
                                    {
                                        CallbackConfigurations = callBackConfigs,
                                        BaseName = regionOptions.DataBaseName,
                                        NamingContext = regionalNamingContext,
                                        DataOptions = dataOptions,
                                        Resources = dataResources,
                                    };

                                    await SimpleDeployExtension.AfterProvisionRegionalDataResourcesAsync.Invoke(parameters);
                                }
                            }

                            _logger.Information("-----------------------------------------------------------------------");
                            _logger.Information($"Successfully finished managing regional data resources.");
                            _logger.Information("-----------------------------------------------------------------------");
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

                            var pip = await WriteReservedIPToDiskAsync(azFactory, aksRGName, aksName, regionalNamingContext, targetOptions, ipPool);
                            if (pip?.Name?.OrdinalContains(IPPoolManager.c_reservedNamePart) == true)
                            {
                                try
                                {
                                    _logger.Information("Granting the Network contrinutor over the public IP '{pipId}' to the AKS SPN with object Id '{AKSobjectId}' ...", pip.Id, targetOptions.AKSConfigurations.AKSSPNObjectId);
                                    await liftrAzure.Authenticated.RoleAssignments
                                        .Define(SdkContext.RandomGuid())
                                        .ForObjectId(targetOptions.AKSConfigurations.AKSSPNObjectId)
                                        .WithBuiltInRole(BuiltInRole.NetworkContributor)
                                        .WithResourceScope(pip)
                                        .CreateAsync();
                                    _logger.Information("Granted Network contrinutor.");
                                }
                                catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                                {
                                }
                                catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
                                {
                                    _logger.Error("The AKS SPN object Id '{AKSobjectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", targetOptions.AKSConfigurations.AKSSPNObjectId);
                                    throw;
                                }
                            }

                            File.WriteAllText("vault-name.txt", kv.Name);
                            File.WriteAllText("aks-domain.txt", $"{aks.Name}.{targetOptions.DomainName}");
                            File.WriteAllText("aks-name.txt", aks.Name);
                            File.WriteAllText("aks-rg.txt", aks.ResourceGroupName);
                            File.WriteAllText("msi-resourceId.txt", msi.Id);
                            File.WriteAllText("msi-clientId.txt", msi.ClientId);

                            _logger.Information("-----------------------------------------------------------------------");
                            _logger.Information($"Successfully finished managing regional compute resources.");
                            _logger.Information("-----------------------------------------------------------------------");
                        }
                        else if (_commandOptions.Action == ActionType.PrepareK8SAppDeployment)
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

                            await WriteReservedIPToDiskAsync(azFactory, aksRGName, aksName, regionalNamingContext, targetOptions, ipPool);

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
                            var tmId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName)}/providers/Microsoft.Network/trafficmanagerprofiles/{regionalNamingContext.TrafficManagerName(regionOptions.DataBaseName)}";

                            var dnsZone = await liftrAzure.GetDNSZoneAsync(globalRGName, targetOptions.DomainName);
                            if (dnsZone == null)
                            {
                                var errMsg = $"Cannot find the DNS zone for domina '{targetOptions.DomainName}' in RG '{globalRGName}'.";
                                _logger.Error(errMsg);
                                throw new InvalidOperationException(errMsg);
                            }

                            var aksHelper = new AKSHelper(_logger);
                            var pip = await aksHelper.GetAKSPublicIPAsync(liftrAzure, aksRGName, aksName, regionalNamingContext.Location);
                            if (pip == null)
                            {
                                var errMsg = $"Cannot find the public Ip address for the AKS cluster. aksRGName:{aksRGName}, aksName:{aksName}, region:{regionalNamingContext.Location}.";
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
                            await SimpleDeployExtension.AfterRunAsync.Invoke(callBackConfigs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is CloudException)
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

        private async Task<IPublicIPAddress> WriteReservedIPToDiskAsync(
            LiftrAzureFactory azFactory,
            string aksRGName,
            string aksName,
            NamingContext regionalNamingContext,
            HostingEnvironmentOptions targetOptions,
            IPPoolManager ipPool)
        {
            if (targetOptions.IPPerRegion == 0)
            {
                return null;
            }

            var aksHelper = new AKSHelper(_logger);
            var pip = await aksHelper.GetAKSPublicIPAsync(azFactory.GenerateLiftrAzure(), aksRGName, aksName, regionalNamingContext.Location);
            if (pip == null)
            {
                pip = await ipPool.GetAvailableIPAsync(regionalNamingContext.Location);
            }

            if (pip != null)
            {
                File.WriteAllText("public-ip.txt", pip.IPAddress);
                File.WriteAllText("public-ip-rg.txt", pip.ResourceGroupName);
            }
            else
            {
                var ex = new InvalidOperationException("There is no available IP address for the AKS cluster to use. Please clean up old ones first.");
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            return pip;
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
