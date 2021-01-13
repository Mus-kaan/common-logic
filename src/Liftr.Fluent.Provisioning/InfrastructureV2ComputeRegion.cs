//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Hosting.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public partial class InfrastructureV2
    {
        public async Task<ProvisionedComputeResources> CreateOrUpdateComputeRegionAsync(
            NamingContext computeNamingContext,
            NamingContext dataNamingContext,
            RegionalComputeOptions computeOptions,
            AKSInfo aksInfo,
            KeyVaultClient _kvClient,
            bool enableVNet,
            IPublicIPAddress outboundIp,
            string allowedAcisExtensions = null,
            bool supportAvailabilityZone = false)
        {
            if (computeNamingContext == null)
            {
                throw new ArgumentNullException(nameof(computeNamingContext));
            }

            if (dataNamingContext == null)
            {
                throw new ArgumentNullException(nameof(dataNamingContext));
            }

            if (computeOptions == null)
            {
                throw new ArgumentNullException(nameof(computeOptions));
            }

            if (aksInfo == null)
            {
                throw new ArgumentNullException(nameof(aksInfo));
            }

            if (outboundIp == null)
            {
                throw new ArgumentNullException(nameof(outboundIp));
            }

            ProvisionedComputeResources provisionedResources = new ProvisionedComputeResources();

            computeOptions.CheckValues();
            aksInfo.CheckValues();

            _logger.Information("AKS machine type: {AKSMachineType}", aksInfo.AKSMachineType.Value);
            _logger.Information("AKS machine count: {AKSMachineCount}", aksInfo.AKSMachineCount);

            var rgName = computeNamingContext.ResourceGroupName(computeOptions.ComputeBaseName);
            var aksName = computeNamingContext.AKSName(computeOptions.ComputeBaseName);
            var currentPublicIP = await MetadataHelper.GetPublicIPAddressAsync();
            _logger.Information("Current public IP address: {currentPublicIP}", currentPublicIP);

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            await liftrAzure.GetOrCreateResourceGroupAsync(computeNamingContext.Location, rgName, computeNamingContext.Tags);
            var dataRGName = dataNamingContext.ResourceGroupName(computeOptions.DataBaseName);
            var vnet = await liftrAzure.GetVNetAsync(dataRGName, dataNamingContext.NetworkName(computeOptions.DataBaseName));
            INetworkSecurityGroup nsg = null;
            if (vnet != null)
            {
                var nsgName = $"{vnet.Name}-default-nsg";
                nsg = await liftrAzure.GetNSGAsync(dataRGName, nsgName);
                if (nsg == null)
                {
                    var ex = new InvalidOperationException($"Cannot the NSG with resource name '{nsgName}' in Resource Group '{dataRGName}'.");
                    _logger.Error("Cannot the NSG with resource name: {nsgName} in {dataRGName}", nsgName, dataRGName);
                    throw ex;
                }
            }

            var subnet = enableVNet ? await liftrAzure.CreateNewSubnetAsync(vnet, computeNamingContext.SubnetName(computeOptions.ComputeBaseName), nsg?.Id) : null;

            var msiName = computeNamingContext.MSIName(computeOptions.ComputeBaseName);
            var msi = await liftrAzure.GetOrCreateMSIAsync(computeNamingContext.Location, rgName, msiName, computeNamingContext.Tags);
            provisionedResources.ManagedIdentity = msi;

            var kvName = computeNamingContext.KeyVaultName(computeOptions.ComputeBaseName);
            if (enableVNet)
            {
                provisionedResources.KeyVault = await liftrAzure.GetKeyVaultAsync(rgName, kvName);
                if (provisionedResources.KeyVault == null)
                {
                    provisionedResources.KeyVault = await liftrAzure.GetOrCreateKeyVaultAsync(computeNamingContext.Location, rgName, kvName, currentPublicIP, computeNamingContext.Tags);
                }
                else
                {
                    _logger.Information("Make sure the Key Vault '{kvId}' can be accessed from current IP '{currentPublicIP}' and subnet '{subnetId}'.", provisionedResources.KeyVault.Id, currentPublicIP, subnet?.Inner?.Id);
                    await liftrAzure.WithKeyVaultAccessFromNetworkAsync(provisionedResources.KeyVault, currentPublicIP, subnet?.Inner?.Id);
                }
            }
            else
            {
                provisionedResources.KeyVault = await liftrAzure.GetOrCreateKeyVaultAsync(computeNamingContext.Location, rgName, kvName, computeNamingContext.Tags);
            }

            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();
            await liftrAzure.GrantSelfKeyVaultAdminAccessAsync(provisionedResources.KeyVault);

            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();
            await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(provisionedResources.KeyVault, computeOptions.LogAnalyticsWorkspaceResourceId);
            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();

            _logger.Information("Start adding access policy for managed identity to regional kv.");
            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();
            await provisionedResources.KeyVault.Update()
                .DefineAccessPolicy()
                .ForObjectId(provisionedResources.ManagedIdentity.GetObjectId())
                .AllowKeyPermissions(KeyPermissions.Get, KeyPermissions.List)
                .AllowSecretPermissions(SecretPermissions.List, SecretPermissions.Get)
                .AllowCertificatePermissions(CertificatePermissions.Get, CertificatePermissions.Getissuers, CertificatePermissions.List)
                .Attach()
                .ApplyAsync();
            _logger.Information("Added access policy for msi to regional kv.");

            var storageName = computeNamingContext.StorageAccountName(computeOptions.ComputeBaseName);
            provisionedResources.StorageAccount = await liftrAzure.GetOrCreateStorageAccountAsync(computeNamingContext.Location, rgName, storageName, computeNamingContext.Tags, subnet?.Inner?.Id);
            await liftrAzure.GrantQueueContributorAsync(provisionedResources.StorageAccount, provisionedResources.ManagedIdentity);
            await liftrAzure.GrantBlobContributorAsync(provisionedResources.StorageAccount, provisionedResources.ManagedIdentity);

            var globalKeyVault = await liftrAzure.GetKeyVaultByIdAsync(computeOptions.GlobalKeyVaultResourceId);
            if (globalKeyVault == null)
            {
                var ex = new InvalidOperationException("Cannot find central key vault with resource Id: " + computeOptions.GlobalKeyVaultResourceId);
                _logger.Error("Cannot find central key vault with resource Id: {ResourceId}", computeOptions.GlobalKeyVaultResourceId);
                throw ex;
            }

            string sshUserName = null;
            string sshPublicKey = null;
            using (var globalKVValet = new KeyVaultConcierge(globalKeyVault.VaultUri, _kvClient, _logger))
            {
                sshUserName = (await globalKVValet.GetSecretAsync(SSHUserNameSecretName))?.Value ?? throw new InvalidOperationException("Cannot find ssh user name in key vault");
                sshPublicKey = (await globalKVValet.GetSecretAsync(SSHPublicKeySecretName))?.Value ?? throw new InvalidOperationException("Cannot find ssh public key in key vault");
            }

            var dbName = dataNamingContext.CosmosDBName(computeOptions.DataBaseName);
            var db = await liftrAzure.GetCosmosDBAsync(dataRGName, dbName);
            if (enableVNet)
            {
                if (db != null)
                {
                    // The cosmos DB service endpoint PUT is not idempotent. PUT the same subnet Id will generate 400.
                    var dbVNetRules = db.VirtualNetworkRules;
                    if (dbVNetRules?.Any((subnetId) => subnetId?.Id?.OrdinalEquals(subnet.Inner.Id) == true) != true)
                    {
                        _logger.Information("Restrict access to cosmos DB with Id '{cosmosDBId}' to subnet '{subnetId}'.", db.Id, subnet.Inner.Id);
                        await db.Update().WithVirtualNetworkRule(vnet.Id, subnet.Name).ApplyAsync();
                    }
                }
            }

            if (computeOptions.EnableThanos)
            {
                var thanosStorageName = computeNamingContext.GenerateCommonName(computeOptions.ComputeBaseName, suffix: "tha", delimiter: string.Empty);
                provisionedResources.ThanosStorageAccount = await liftrAzure.GetOrCreateStorageAccountAsync(computeNamingContext.Location, rgName, thanosStorageName, computeNamingContext.Tags);
            }

            provisionedResources.AKS = await liftrAzure.GetAksClusterAsync(rgName, aksName);
            if (provisionedResources.AKS == null)
            {
                var agentPoolName = (computeNamingContext.ShortPartnerName + computeNamingContext.ShortEnvironmentName + computeNamingContext.Location.ShortName()).ToLowerInvariant();
                if (agentPoolName.Length > 11)
                {
                    agentPoolName = agentPoolName.Substring(0, 11);
                }

                string outboundIpId = outboundIp.Id;

                _logger.Information("Computed AKS agent pool profile name: {agentPoolName}", agentPoolName);

                _logger.Information("Creating AKS cluster ...");
                provisionedResources.AKS = await liftrAzure.CreateAksClusterAsync(
                    computeNamingContext.Location,
                    rgName,
                    aksName,
                    sshUserName,
                    sshPublicKey,
                    aksInfo.AKSMachineType,
                    aksInfo.KubernetesVersion,
                    aksInfo.AKSMachineCount,
                    outboundIpId,
                    computeNamingContext.Tags,
                    subnet,
                    agentPoolProfileName: agentPoolName,
                    supportAvailabilityZone);

                _logger.Information("Created AKS cluster with Id {ResourceId}", provisionedResources.AKS.Id);

                // Wait for 8 minutes for AKS to finish the actual supporting resource creation.
                await Task.Delay(TimeSpan.FromMinutes(8));
            }
            else
            {
                _logger.Information("Use existing AKS cluster (ProvisioningState: {ProvisioningState}) with Id '{ResourceId}'.", provisionedResources.AKS.ProvisioningState, provisionedResources.AKS.Id);
            }

            if (!string.IsNullOrEmpty(computeOptions.LogAnalyticsWorkspaceResourceId))
            {
                var aksAddOns = new Dictionary<string, ManagedClusterAddonProfile>()
                {
                    ["omsagent"] = new ManagedClusterAddonProfile(true, new Dictionary<string, string>()
                    {
                        ["logAnalyticsWorkspaceResourceID"] = computeOptions.LogAnalyticsWorkspaceResourceId,
                    }),
                };
                _logger.Information("Enable AKS Azure Monitor and send the diagnostics data to Log Analytics with Id '{logAnalyticsWorkspaceResourceId}'", computeOptions.LogAnalyticsWorkspaceResourceId);
                await provisionedResources.AKS.Update().WithAddOnProfiles(aksAddOns).ApplyAsync();
            }

            provisionedResources.AKSObjectId = await liftrAzure.GetAKSMIAsync(rgName, aksName);
            if (string.IsNullOrEmpty(provisionedResources.AKSObjectId))
            {
                var errMsg = "Cannot find the system assigned managed identity of the AKS cluster: " + provisionedResources.AKS.Id;
                var ex = new InvalidOperationException(errMsg);
                _logger.Error(ex, errMsg);
                throw ex;
            }

            IIdentity kubeletMI = null;
            var delayCounter = 0;
            const int AKSMIListingMaxDelayCounter = 20;

            while (kubeletMI is null && delayCounter < AKSMIListingMaxDelayCounter)
            {
                _logger.Information($"Waiting for 2 minutes before listing kubelet MI for AKS {aksName} and resourcegroup {rgName}");

                // Wait for 2 minutes to ensure the AKS MIs can be listed.
                await Task.Delay(TimeSpan.FromMinutes(2));

                var mcMIList = await liftrAzure.ListAKSMCMIAsync(rgName, aksName, computeNamingContext.Location);

                // e.g. sp-test-com20200608-wus2-aks-agentpool
                kubeletMI = mcMIList.FirstOrDefault(id => id.Name.OrdinalStartsWith(aksName));
            }

            if (kubeletMI == null)
            {
                var errMsg = $"Cannot find the kubelet managed identity. There should be exactly one kubelet MI for aks: '{provisionedResources.AKS.Id}'. If the AKS cluster is just created, please wait for several minutes and retry.";
                var ex = new InvalidOperationException(errMsg);
                _logger.Error(ex, errMsg);
                throw ex;
            }

            provisionedResources.KubeletObjectId = kubeletMI.GetObjectId();

            try
            {
                // Managed Identity Operator
                var roleDefinitionId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/c7393b34-138c-406f-901b-d8cf2b17e6ae";
                _logger.Information("Granting Managed Identity Operator for the MSI {MSIResourceId} to the kubelet MI with object Id '{AKSobjectId}' ...", msi.Id, provisionedResources.KubeletObjectId);
                await liftrAzure.Authenticated.RoleAssignments
                    .Define(SdkContext.RandomGuid())
                    .ForObjectId(provisionedResources.KubeletObjectId)
                    .WithBuiltInRole(BuiltInRole.Contributor)
                    .WithResourceScope(msi)
                    .CreateAsync();
                _logger.Information("Granted Managed Identity Operator for the MSI {MSIResourceId} to the kubelet MI with object Id '{AKSobjectId}'.", msi.Id, provisionedResources.KubeletObjectId);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }

            // Assign Virtual Machine Contributor to the kublet MI over the MC_ resource group. It need this to bind MI to the VM/VMSS.
            try
            {
                // Virtual Machine Contributor
                var roleDefinitionId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/9980e02c-c2be-4d73-94e8-173b1dc7cf3c";
                var mcRGName = NamingContext.AKSMCResourceGroupName(rgName, aksName, computeNamingContext.Location);
                var mcRG = await liftrAzure.GetResourceGroupAsync(mcRGName);
                _logger.Information("Granting the contributor role over the AKS MC_ RG '{mcRGName}' to the kubelet MI with object Id '{kubeletObjectId}' ...", mcRGName, provisionedResources.KubeletObjectId);
                await liftrAzure.Authenticated.RoleAssignments
                    .Define(SdkContext.RandomGuid())
                    .ForObjectId(provisionedResources.KubeletObjectId)
                    .WithBuiltInRole(BuiltInRole.Contributor)
                    .WithResourceGroupScope(mcRG)
                    .CreateAsync();
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }

            if (subnet != null)
            {
                try
                {
                    _logger.Information("Make sure the AKS MI '{AKSSPNObjectId}' has write access to the subnet '{subnetId}'.", provisionedResources.AKSObjectId, subnet.Inner.Id);
                    await liftrAzure.Authenticated.RoleAssignments
                        .Define(SdkContext.RandomGuid())
                        .ForObjectId(provisionedResources.AKSObjectId)
                        .WithBuiltInRole(BuiltInRole.NetworkContributor)
                        .WithScope(subnet.Inner.Id)
                        .CreateAsync();
                    _logger.Information("Network contributor role is assigned to the AKS MI '{AKSSPNObjectId}' for the subnet '{subnetId}'.", provisionedResources.AKSObjectId, subnet.Inner.Id);
                }
                catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                {
                }
            }

            IStorageAccount asicStorage = null;
            if (!string.IsNullOrEmpty(allowedAcisExtensions))
            {
                var acis = new ACISProvision(_azureClientFactory, _kvClient, _logger, allowedAcisExtensions);
                asicStorage = await acis.ProvisionACISResourcesAsync(computeNamingContext);
                await liftrAzure.GrantQueueContributorAsync(asicStorage, provisionedResources.ManagedIdentity);
            }

            provisionedResources.RPAssetOptions = await AddKeyVaultSecretsAsync(
                computeNamingContext,
                provisionedResources.KeyVault,
                computeOptions.SecretPrefix,
                provisionedResources.StorageAccount,
                computeOptions.ActiveDBKeyName,
                db,
                computeOptions.GlobalStorageResourceId,
                computeOptions.GlobalKeyVaultResourceId,
                provisionedResources.ManagedIdentity,
                asicStorage,
                computeOptions.GlobalCosmosDBResourceId);

            var sslSubjects = new List<string>()
            {
                computeOptions.DomainName,
                $"*.{computeOptions.DomainName}",
                $"{dataNamingContext.Location.ShortName()}.{computeOptions.DomainName}",
                $"*.{dataNamingContext.Location.ShortName()}.{computeOptions.DomainName}",
                $"{computeNamingContext.Location.ShortName()}.{computeOptions.DomainName}",
                $"*.{computeNamingContext.Location.ShortName()}.{computeOptions.DomainName}",
            };

            using (var regionalKVValet = new KeyVaultConcierge(provisionedResources.KeyVault.VaultUri, _kvClient, _logger))
            {
                await CreateKeyVaultCertificatesAsync(regionalKVValet, computeOptions.OneCertCertificates, sslSubjects, computeNamingContext.Tags);
            }

            return provisionedResources;
        }
    }
}
