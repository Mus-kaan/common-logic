﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
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
        public async Task<(IVault kv, IIdentity msi, IKubernetesCluster aks, string aksObjectId, string kubeletObjectId)> CreateOrUpdateRegionalAKSRGAsync(
            NamingContext namingContext,
            RegionalComputeOptions computeOptions,
            AKSInfo aksInfo,
            KeyVaultClient _kvClient,
            bool enableVNet)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            if (computeOptions == null)
            {
                throw new ArgumentNullException(nameof(computeOptions));
            }

            if (aksInfo == null)
            {
                throw new ArgumentNullException(nameof(aksInfo));
            }

            _logger.Information("InfraV2RegionalComputeOptions: {@InfraV2RegionalComputeOptions}", computeOptions);
            _logger.Information("AKSInfo: {@AKSInfo}", aksInfo);
            computeOptions.CheckValues();
            aksInfo.CheckValues();

            _logger.Information("AKS machine type: {AKSMachineType}", aksInfo.AKSMachineType.Value);
            _logger.Information("AKS machine count: {AKSMachineCount}", aksInfo.AKSMachineCount);

            var rgName = namingContext.ResourceGroupName(computeOptions.ComputeBaseName);
            var aksName = namingContext.AKSName(computeOptions.ComputeBaseName);
            var currentPublicIP = await MetadataHelper.GetPublicIPAddressAsync();
            _logger.Information("Current public IP address: {currentPublicIP}", currentPublicIP);

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
            var dataRGName = namingContext.ResourceGroupName(computeOptions.DataBaseName);
            var vnet = await liftrAzure.GetVNetAsync(dataRGName, namingContext.NetworkName(computeOptions.DataBaseName));
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

            var msiName = namingContext.MSIName(computeOptions.DataBaseName);
            var msi = await liftrAzure.GetMSIAsync(namingContext.ResourceGroupName(computeOptions.DataBaseName), msiName);
            if (msi == null)
            {
                var ex = new InvalidOperationException("Cannot find regional MSI with resource name: " + msiName);
                _logger.Error("Cannot find regional MSI with resource name: {ResourceName}", msiName);
                throw ex;
            }

            var kvName = namingContext.KeyVaultName(computeOptions.DataBaseName);
            var regionalKeyVault = await liftrAzure.GetKeyVaultAsync(namingContext.ResourceGroupName(computeOptions.DataBaseName), kvName);
            if (regionalKeyVault == null)
            {
                var ex = new InvalidOperationException("Cannot find regional key vault with resource name: " + kvName);
                _logger.Error("Cannot find regional key vault with resource name: {ResourceName}", kvName);
                throw ex;
            }

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

            var subnet = enableVNet ? await liftrAzure.CreateNewSubnetAsync(vnet, namingContext.SubnetName(computeOptions.ComputeBaseName), nsg?.Id) : null;

            if (enableVNet)
            {
                _logger.Information("Restrict the Key Vault '{kvId}' to IP '{currentPublicIP}' and subnet '{subnetId}'.", regionalKeyVault.Id, currentPublicIP, subnet?.Inner?.Id);
                await liftrAzure.WithKeyVaultAccessFromNetworkAsync(regionalKeyVault, currentPublicIP, subnet?.Inner?.Id);

                var storName = namingContext.StorageAccountName(computeOptions.DataBaseName);
                var stor = await liftrAzure.GetStorageAccountAsync(dataRGName, storName);
                if (stor != null)
                {
                    _logger.Information("Restrict access to storage account with Id '{storId}' to subnet '{subnetId}'.", stor.Id, subnet.Inner.Id);
                    await stor.Update().WithAccessFromNetworkSubnet(subnet.Inner.Id).ApplyAsync();
                }

                var dbName = namingContext.CosmosDBName(computeOptions.DataBaseName);
                var db = await liftrAzure.GetCosmosDBAsync(dataRGName, dbName);
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

            var aks = await liftrAzure.GetAksClusterAsync(rgName, aksName);
            if (aks == null)
            {
                var agentPoolName = (namingContext.ShortPartnerName + namingContext.ShortEnvironmentName + namingContext.Location.ShortName()).ToLowerInvariant();
                if (agentPoolName.Length > 11)
                {
                    agentPoolName = agentPoolName.Substring(0, 11);
                }

                _logger.Information("Computed AKS agent pool profile name: {agentPoolName}", agentPoolName);

                _logger.Information("Creating AKS cluster ...");
                aks = await liftrAzure.CreateAksClusterAsync(
                    namingContext.Location,
                    rgName,
                    aksName,
                    sshUserName,
                    sshPublicKey,
                    aksInfo.AKSMachineType,
                    aksInfo.AKSMachineCount,
                    namingContext.Tags,
                    subnet,
                    agentPoolProfileName: agentPoolName);

                _logger.Information("Created AKS cluster with Id {ResourceId}", aks.Id);
            }
            else
            {
                _logger.Information("Use existing AKS cluster (ProvisioningState: {ProvisioningState}) with Id '{ResourceId}'.", aks.ProvisioningState, aks.Id);
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
                await aks.Update().WithAddOnProfiles(aksAddOns).ApplyAsync();
            }

            var aksMIObjectId = await liftrAzure.GetAKSMIAsync(rgName, aksName);
            if (string.IsNullOrEmpty(aksMIObjectId))
            {
                var errMsg = "Cannot find the system assigned managed identity of the AKS cluster: " + aks.Id;
                var ex = new InvalidOperationException(errMsg);
                _logger.Error(ex, errMsg);
                throw ex;
            }

            var mcMIList = await liftrAzure.ListAKSMCMIAsync(rgName, aksName, namingContext.Location);

            // e.g. sp-test-com20200608-wus2-aks-agentpool
            var kubeletMI = mcMIList.FirstOrDefault(id => id.Name.OrdinalStartsWith(aksName));
            if (kubeletMI == null)
            {
                var errMsg = "There should be exactly one kubelet MI for aks: " + aks.Id;
                var ex = new InvalidOperationException(errMsg);
                _logger.Error(ex, errMsg);
                throw ex;
            }

            var kubeletObjectId = kubeletMI.GetObjectId();

            try
            {
                // Managed Identity Operator
                var roleDefinitionId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/c7393b34-138c-406f-901b-d8cf2b17e6ae";
                _logger.Information("Granting Managed Identity Operator for the MSI {MSIResourceId} to the kubelet MI with object Id '{AKSobjectId}' ...", msi.Id, kubeletObjectId);
                await liftrAzure.Authenticated.RoleAssignments
                    .Define(SdkContext.RandomGuid())
                    .ForObjectId(kubeletObjectId)
                    .WithBuiltInRole(BuiltInRole.Contributor)
                    .WithResourceScope(msi)
                    .CreateAsync();
                _logger.Information("Granted Managed Identity Operator for the MSI {MSIResourceId} to the kubelet MI with object Id '{AKSobjectId}'.", msi.Id, kubeletObjectId);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }

            // Assign Virtual Machine Contributor to the kublet MI over the MC_ resource group. It need this to bind MI to the VM/VMSS.
            try
            {
                // Virtual Machine Contributor
                var roleDefinitionId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/9980e02c-c2be-4d73-94e8-173b1dc7cf3c";
                var mcRGName = NamingContext.AKSMCResourceGroupName(rgName, aksName, namingContext.Location);
                var mcRG = await liftrAzure.GetResourceGroupAsync(mcRGName);
                _logger.Information("Granting the contributor role over the AKS MC_ RG '{mcRGName}' to the kubelet MI with object Id '{kubeletObjectId}' ...", mcRGName, kubeletObjectId);
                await liftrAzure.Authenticated.RoleAssignments
                    .Define(SdkContext.RandomGuid())
                    .ForObjectId(kubeletObjectId)
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
                    _logger.Information("Make sure the AKS MI '{AKSSPNObjectId}' has write access to the subnet '{subnetId}'.", aksMIObjectId, subnet.Inner.Id);
                    await liftrAzure.Authenticated.RoleAssignments
                        .Define(SdkContext.RandomGuid())
                        .ForObjectId(aksMIObjectId)
                        .WithBuiltInRole(BuiltInRole.NetworkContributor)
                        .WithScope(subnet.Inner.Id)
                        .CreateAsync();
                    _logger.Information("Network contributor role is assigned to the AKS MI '{AKSSPNObjectId}' for the subnet '{subnetId}'.", aksMIObjectId, subnet.Inner.Id);
                }
                catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                {
                }
            }

            return (regionalKeyVault, msi, aks, aksMIObjectId, kubeletObjectId);
        }
    }
}