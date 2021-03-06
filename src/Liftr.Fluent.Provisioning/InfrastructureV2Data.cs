//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public partial class InfrastructureV2
    {
        public async Task<ProvisionedRegionalDataResources> CreateOrUpdateRegionalDataRGAsync(
            string baseName,
            NamingContext namingContext,
            RegionalDataOptions dataOptions,
            bool createVNet,
            string allowedAcisExtensions = null)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            if (dataOptions == null)
            {
                throw new ArgumentNullException(nameof(dataOptions));
            }

            dataOptions.CheckValid();

            var dataRegionResources = await CreateOrUpdateDataRegionAsync(baseName, namingContext, dataOptions, createVNet);

            ProvisionedRegionalDataResources provisionedResources = new ProvisionedRegionalDataResources()
            {
                ResourceGroup = dataRegionResources.ResourceGroup,
                GlobalResourceGroup = dataRegionResources.GlobalResourceGroup,
                VNet = dataRegionResources.VNet,
                DnsZone = dataRegionResources.DnsZone,
                TrafficManager = dataRegionResources.TrafficManager,
                CosmosDBAccount = dataRegionResources.CosmosDBAccount,
            };

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            var rgName = namingContext.ResourceGroupName(baseName);
            var storageName = namingContext.StorageAccountName(baseName);
            var kvName = namingContext.KeyVaultName(baseName);
            var msiName = namingContext.MSIName(baseName);
            var currentPublicIP = await MetadataHelper.GetPublicIPAddressAsync();
            _logger.Information("Current public IP address: {currentPublicIP}", currentPublicIP);

            var rg = await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
            provisionedResources.ResourceGroup = rg;
            provisionedResources.ManagedIdentity = await liftrAzure.GetOrCreateMSIAsync(namingContext.Location, rgName, msiName, namingContext.Tags);

            ISubnet subnet = null;
            if (dataOptions.EnableVNet)
            {
                subnet = provisionedResources.VNet.Subnets[liftrAzure.DefaultSubnetName];
                provisionedResources.KeyVault = await liftrAzure.GetKeyVaultAsync(rgName, kvName);
                if (provisionedResources.KeyVault == null)
                {
                    provisionedResources.KeyVault = await liftrAzure.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, currentPublicIP, namingContext.Tags);
                }
                else
                {
                    _logger.Information("Make sure the Key Vault '{kvId}' can be accessed from current IP '{currentPublicIP}'.", provisionedResources.KeyVault.Id, currentPublicIP);
                    await liftrAzure.WithKeyVaultAccessFromNetworkAsync(provisionedResources.KeyVault, currentPublicIP, subnet?.Inner?.Id);
                }
            }
            else
            {
                provisionedResources.KeyVault = await liftrAzure.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags);
            }

            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();
            await liftrAzure.GrantSelfKeyVaultAdminAccessAsync(provisionedResources.KeyVault);

            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();
            await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(provisionedResources.KeyVault, dataOptions.LogAnalyticsWorkspaceId);
            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();

            provisionedResources.StorageAccount = await liftrAzure.GetOrCreateStorageAccountAsync(namingContext.Location, rgName, storageName, namingContext.Tags, subnet?.Inner?.Id);
            await liftrAzure.GrantQueueContributorAsync(provisionedResources.StorageAccount, provisionedResources.ManagedIdentity);
            await liftrAzure.GrantBlobContributorAsync(provisionedResources.StorageAccount, provisionedResources.ManagedIdentity);

            provisionedResources.StorageAccount = await provisionedResources.StorageAccount.RemoveUnusedVNetRulesAsync(_azureClientFactory, _logger);
            if (dataOptions.EnableVNet)
            {
                _logger.Information("Make sure the Storage Account '{saId}' can be accessed from current IP '{currentPublicIP}'.", provisionedResources.StorageAccount.Id, currentPublicIP);
                await provisionedResources.StorageAccount.WithAccessFromIpAddressAsync(currentPublicIP, _logger);
            }

            if (dataOptions.EnableThanos)
            {
                var thanosStorageName = namingContext.ThanosStorageAccountName(baseName);
                provisionedResources.ThanosStorageAccount = await liftrAzure.GetOrCreateStorageAccountAsync(namingContext.Location, rgName, thanosStorageName, namingContext.Tags);
            }

            if (dataOptions.DataPlaneSubscriptions != null)
            {
                foreach (var subscrptionId in dataOptions.DataPlaneSubscriptions)
                {
                    try
                    {
                        _logger.Information("Granting the MSI {MSIReourceId} Owner role to the subscription with {subscrptionId} ...", provisionedResources.ManagedIdentity.Id, subscrptionId);
                        await liftrAzure.Authenticated.RoleAssignments
                            .Define(SdkContext.RandomGuid())
                            .ForObjectId(provisionedResources.ManagedIdentity.GetObjectId())
                            .WithBuiltInRole(BuiltInRole.Owner)
                            .WithSubscriptionScope(subscrptionId)
                            .CreateAsync();
                    }
                    catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                    {
                    }

                    await liftrAzure.GrantBlobContributorAsync(subscrptionId, provisionedResources.ManagedIdentity);
                }
            }

            _logger.Information("Start adding access policy for managed identity to regional kv.");
            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();
            await provisionedResources.KeyVault.Update()
                .DefineAccessPolicy()
                .ForObjectId(provisionedResources.ManagedIdentity.GetObjectId())
                .AllowKeyPermissions(KeyPermissions.WrapKey, KeyPermissions.UnwrapKey, KeyPermissions.Get, KeyPermissions.List)
                .AllowSecretPermissions(SecretPermissions.List, SecretPermissions.Get)
                .AllowCertificatePermissions(CertificatePermissions.Get, CertificatePermissions.Getissuers, CertificatePermissions.List)
                .Attach()
                .ApplyAsync();
            _logger.Information("Added access policy for msi to regional kv.");

            IStorageAccount asicStorage = null;
            if (!string.IsNullOrEmpty(allowedAcisExtensions))
            {
                var acis = new ACISProvision(_azureClientFactory, _kvClient, _logger, allowedAcisExtensions);
                asicStorage = await acis.ProvisionACISResourcesAsync(namingContext);
                await liftrAzure.GrantQueueContributorAsync(asicStorage, provisionedResources.ManagedIdentity);
            }

            provisionedResources.DataAssetOptions = await AddKeyVaultSecretsAsync(
                namingContext,
                provisionedResources.KeyVault,
                dataOptions.SecretPrefix,
                provisionedResources.StorageAccount,
                provisionedResources.CosmosDBAccount,
                dataOptions.GlobalStorageResourceId,
                dataOptions.GlobalKeyVaultResourceId,
                provisionedResources.ManagedIdentity,
                asicStorage,
                dataOptions.GlobalCosmosDBResourceId,
                dataOptions.DataPlaneSubscriptions,
                dataOptions.OutboundIPList);

            var sslSubjects = new List<string>()
            {
                dataOptions.DomainName,
                $"*.{dataOptions.DomainName}",
                $"{namingContext.Location.ShortName()}.{dataOptions.DomainName}",
                $"*.{namingContext.Location.ShortName()}.{dataOptions.DomainName}",
            };

            using (var regionalKVValet = new KeyVaultConcierge(provisionedResources.KeyVault.VaultUri, _kvClient, _logger))
            {
                await CreateKeyVaultCertificatesAsync(regionalKVValet, dataOptions.OneCertCertificates, sslSubjects, namingContext.Tags);
            }

            return provisionedResources;
        }
    }
}
