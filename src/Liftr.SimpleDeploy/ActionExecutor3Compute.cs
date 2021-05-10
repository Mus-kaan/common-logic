//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Hosting.Contracts;
using Microsoft.Rest.Azure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.SimpleDeploy
{
    public sealed partial class ActionExecutor : IHostedService
    {
        private async Task ManageComputeResourcesAsync(
            HostingEnvironmentOptions targetOptions,
            KeyVaultClient kvClient,
            LiftrAzureFactory azFactory,
            string allowedAcisExtensions)
        {
            var liftrAzure = azFactory.GenerateLiftrAzure();
            var infra = new InfrastructureV2(azFactory, kvClient, _logger);
            var globalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, targetOptions.Global.Location);
            var globalRGName = globalNamingContext.ResourceGroupName(targetOptions.Global.BaseName);
            File.WriteAllText("global-vault-name.txt", globalNamingContext.KeyVaultName(targetOptions.Global.BaseName));

            IPPoolManager ipPool = null;

            var aksHelper = new AKSNetworkHelper(_logger);

            if (targetOptions.IPPerRegion > 0)
            {
                var ipNamePrefix = globalNamingContext.GenerateCommonName(targetOptions.Global.BaseName, noRegion: true);
                ipPool = new IPPoolManager(ipNamePrefix, azFactory, _logger);
            }

            var parsedRegionInfo = GetRegionalOptions(targetOptions);
            var regionOptions = parsedRegionInfo.RegionOptions;
            var regionalNamingContext = parsedRegionInfo.RegionNamingContext;
            var aksRGName = parsedRegionInfo.AKSRGName;
            var aksName = parsedRegionInfo.AKSName;
            var aksRegion = parsedRegionInfo.AKSRegion;
            var enableAKSAvailabilityZone = parsedRegionInfo.EnableAvailabilityZone;
            regionalNamingContext.Tags["GlobalRG"] = globalRGName;

            RegionalComputeOptions regionalComputeOptions = new RegionalComputeOptions()
            {
                DataBaseName = regionOptions.DataBaseName,
                ComputeBaseName = regionOptions.ComputeBaseName,
                GlobalKeyVaultResourceId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.KeyVault/vaults/{globalNamingContext.KeyVaultName(targetOptions.Global.BaseName)}",
                LogAnalyticsWorkspaceResourceId = targetOptions.LogAnalyticsWorkspaceId,
                ActiveDBKeyName = _commandOptions.ActiveKeyName,
                SecretPrefix = _hostingOptions.SecretPrefix,
                GlobalStorageResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.Storage/storageAccounts/{globalNamingContext.StorageAccountName(targetOptions.Global.BaseName)}",
                GlobalCosmosDBResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.DocumentDB/databaseAccounts/{globalNamingContext.CosmosDBName(targetOptions.Global.BaseName)}",
                DomainName = targetOptions.DomainName,
                ZoneRedundant = regionOptions.ZoneRedundant,
                OneCertCertificates = targetOptions.OneCertCertificates,
                EnableThanos = _hostingOptions.EnableThanos,
            };

            regionalNamingContext.Tags["DataRG"] = regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName);

            var dataOptions = new RegionalDataOptions()
            {
                ActiveDBKeyName = _commandOptions.ActiveKeyName,
                SecretPrefix = _hostingOptions.SecretPrefix,
                OneCertCertificates = targetOptions.OneCertCertificates,
                DataPlaneSubscriptions = regionOptions.DataPlaneSubscriptions,
                DataPlaneStorageCountPerSubscription = _hostingOptions.StorageCountPerDataPlaneSubscription,
                EnableVNet = targetOptions.EnableVNet,
                DBSupport = _hostingOptions.DBSupport,
                GlobalKeyVaultResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.KeyVault/vaults/{globalNamingContext.KeyVaultName(targetOptions.Global.BaseName)}",
                GlobalStorageResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.Storage/storageAccounts/{globalNamingContext.StorageAccountName(targetOptions.Global.BaseName)}",
                LogAnalyticsWorkspaceId = targetOptions.LogAnalyticsWorkspaceId,
                DomainName = targetOptions.DomainName,
                DNSZoneId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{globalRGName}/providers/Microsoft.Network/dnszones/{targetOptions.DomainName}",
            };

            bool createVNet = targetOptions.IsAKS ? targetOptions.EnableVNet : true;

            regionalNamingContext.Tags["DataRG"] = regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName);

            var acr = await infra.GetACRAsync(targetOptions.Global.BaseName, globalNamingContext);
            if (acr == null)
            {
                var errMsg = "Cannot find the global ACR.";
                _logger.Fatal(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            IPublicIPAddress outboundIpAddress = null;

            if (targetOptions.AKSConfigurations != null)
            {
                ProvisionedComputeResources computeResources = null;

                // Find Outbound Public IP for AKS cluster creation
                if (targetOptions.IPPerRegion >= 3)
                {
                    // Check if Outbound Public IP under AKS network already exists
                    try
                    {
                        outboundIpAddress = await aksHelper.GetAKSPublicIPAsync(liftrAzure, aksRGName, aksName, aksRegion, IPCategory.Outbound);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.Information($"Public IP is not available from AKS Network as AKS Cluster doesn't exist in region {aksRegion}. Reason: {ex.Message}. We can go ahead for Outbound Public IP creation for AKS.");
                    }

                    if (outboundIpAddress == null)
                    {
                        outboundIpAddress = await ipPool.GetAvailableIPAsync(aksRegion, IPCategory.Outbound);

                        if (outboundIpAddress == null)
                        {
                            var ex = new InvalidOperationException($"Cannot get available IP address for region {aksRegion}");
                            _logger.Error(ex, ex.Message);
                            throw ex;
                        }
                    }

                    _logger.Information($"Outbound IP address {outboundIpAddress?.IPAddress} created for the AKS Cluster...");
                }

                if (!string.IsNullOrEmpty(regionOptions.KubernetesVersion))
                {
                    _logger.Information(
                        "For region {aksRegion}, overwritting the default k8s version {defaultK8sVersion} with regional version {k8sVersion}",
                        regionalNamingContext.Location.Name,
                        targetOptions.AKSConfigurations.KubernetesVersion,
                        regionOptions.KubernetesVersion);
                    targetOptions.AKSConfigurations.KubernetesVersion = regionOptions.KubernetesVersion;
                }

                computeResources = await infra.CreateOrUpdateRegionalAKSRGAsync(
                    regionalNamingContext,
                    regionalComputeOptions,
                    targetOptions.AKSConfigurations,
                    kvClient,
                    targetOptions.EnableVNet,
                    outboundIpAddress,
                    enableAKSAvailabilityZone);

                if (computeResources.ThanosStorageAccount != null)
                {
                    // write the Thanos storage credential to disk so the helm deployment can utilize it.
                    var storageCredentailManager = new StorageAccountCredentialLifeCycleManager(computeResources.ThanosStorageAccount, new SystemTimeSource(), _logger);
                    var storKey = await storageCredentailManager.GetActiveKeyAsync();
                    File.WriteAllText("diag-stor-name.txt", computeResources.ThanosStorageAccount.Name);
                    File.WriteAllText("diag-stor-key.txt", storKey.Value);
                }

                try
                {
                    // ACR Pull
                    var roleDefinitionId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/7f951dda-4ed3-4680-a7ca-43fe172d538d";
                    _logger.Information("Granting ACR pull role to the kubelet MI over the acr '{acrLogin}' ...", acr.LoginServerUrl);
                    await liftrAzure.Authenticated.RoleAssignments
                        .Define(SdkContext.RandomGuid())
                        .ForObjectId(computeResources.KubeletObjectId)
                        .WithRoleDefinition(roleDefinitionId)
                        .WithResourceScope(acr)
                        .CreateAsync();
                }
                catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                {
                }

                IPublicIPAddress inboundIpAddress = null;

                _logger.Information($"Writing Inbound Public IP to disk on file public-ip.txt for deployment of AKS {aksName} under resource group {aksRGName}");
                inboundIpAddress = await WriteReservedInboundIPToDiskAsync(azFactory, aksRGName, aksName, parsedRegionInfo.AKSRegion, targetOptions, ipPool);

                await GrantNetworkContributorRoleAsync(inboundIpAddress, computeResources, liftrAzure);

                File.WriteAllText("vault-name.txt", computeResources.KeyVault.Name);
                File.WriteAllText("aks-kv.txt", computeResources.KeyVault.VaultUri);
                File.WriteAllText("aks-domain.txt", $"{computeResources.AKS.Name}.{targetOptions.DomainName}");
                File.WriteAllText("aks-name.txt", computeResources.AKS.Name);
                File.WriteAllText("aks-rg.txt", computeResources.AKS.ResourceGroupName);
                File.WriteAllText("msi-resourceId.txt", computeResources.ManagedIdentity.Id);
                File.WriteAllText("msi-clientId.txt", computeResources.ManagedIdentity.ClientId);

                if (SimpleDeployExtension.AfterProvisionRegionalAKSResourcesAsync != null)
                {
                    using (_logger.StartTimedOperation(nameof(SimpleDeployExtension.AfterProvisionRegionalAKSResourcesAsync)))
                    {
                        var parameters = new AKSCallbackParameters()
                        {
                            CallbackConfigurations = _callBackConfigs,
                            BaseName = regionalComputeOptions.ComputeBaseName,
                            NamingContext = regionalNamingContext,
                            ComputeOptions = regionalComputeOptions,
                            RegionOptions = regionOptions,
                            Resources = computeResources,
                            IPPoolManager = ipPool,
                        };

                        await SimpleDeployExtension.AfterProvisionRegionalAKSResourcesAsync.Invoke(parameters);
                    }
                }
            }
            else
            {
                var certNameList = targetOptions.OneCertCertificates.Select(kvp => kvp.Key).ToList();
                certNameList.Add(CertificateName.DefaultSSL);
                if (targetOptions.OneCertCertificates?.ContainsKey(CertificateName.GenevaClientCert) == true)
                {
                    targetOptions.Geneva.GENEVA_CERT_SAN = targetOptions.OneCertCertificates[CertificateName.GenevaClientCert];
                }

                var vmss = await infra.CreateOrUpdateRegionalVMSSRGAsync(
                    regionalNamingContext,
                    regionalComputeOptions,
                    targetOptions.VMSSConfigurations,
                    targetOptions.Geneva,
                    kvClient,
                    ipPool,
                    targetOptions.EnableVNet,
                    certNameList);

                try
                {
                    // ACR Pull
                    var roleDefinitionId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/7f951dda-4ed3-4680-a7ca-43fe172d538d";
                    _logger.Information("Granting ACR pull role to the VMSS over the acr '{acrLogin}' ...", acr.LoginServerUrl);
                    await liftrAzure.Authenticated.RoleAssignments
                        .Define(SdkContext.RandomGuid())
                        .ForObjectId(vmss.ManagedIdentity.GetObjectId())
                        .WithRoleDefinition(roleDefinitionId)
                        .WithResourceScope(acr)
                        .CreateAsync();
                }
                catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                {
                }

                if (SimpleDeployExtension.AfterProvisionRegionalVMSSResourcesAsync != null)
                {
                    using (_logger.StartTimedOperation(nameof(SimpleDeployExtension.AfterProvisionRegionalVMSSResourcesAsync)))
                    {
                        var parameters = new VMSSCallbackParameters()
                        {
                            CallbackConfigurations = _callBackConfigs,
                            BaseName = regionOptions.DataBaseName,
                            NamingContext = regionalNamingContext,
                            ComputeOptions = regionalComputeOptions,
                            RegionOptions = regionOptions,
                            Resources = vmss,
                        };

                        await SimpleDeployExtension.AfterProvisionRegionalVMSSResourcesAsync.Invoke(parameters);
                    }

                    if (targetOptions.EnableVNet)
                    {
                        _logger.Information("Wait 5 minutes and restart VMSS to pick up potential VNet change.");
                        await Task.Delay(TimeSpan.FromMinutes(5));
                        var vm = await vmss.VMSS.RefreshAsync();

                        // fire and forget
                        _ = vm.RestartAsync();
                        await Task.Delay(5000);
                    }
                }
            }

            _logger.Information("-----------------------------------------------------------------------");
            _logger.Information($"Successfully finished managing regional compute resources.");
            _logger.Information("-----------------------------------------------------------------------");
        }

        private async Task GrantNetworkContributorRoleAsync(IPublicIPAddress pip, ProvisionedComputeResources computeResources, ILiftrAzure liftrAzure)
        {
            if (pip?.Name?.OrdinalContains(IPPoolManager.c_reservedNamePart) == true)
            {
                try
                {
                    _logger.Information($"Granting the Network contributor over the public IP {pip?.IPAddress} Id: {pip?.Id} to the AKS SPN with object Id {computeResources.AKSObjectId} ...");
                    await liftrAzure.Authenticated.RoleAssignments
                        .Define(SdkContext.RandomGuid())
                        .ForObjectId(computeResources.AKSObjectId)
                        .WithBuiltInRole(BuiltInRole.NetworkContributor)
                        .WithResourceScope(pip)
                        .CreateAsync();
                    _logger.Information($"Granted Network contributor Role to public IP {pip?.IPAddress} Id: {pip?.Id} to the AKS SPN with object Id {computeResources.AKSObjectId} ...");
                }
                catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                {
                }
                catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
                {
                    _logger.Error("The AKS SPN object Id '{AKSobjectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", computeResources.AKSObjectId);
                    throw;
                }
            }
        }
    }
}
