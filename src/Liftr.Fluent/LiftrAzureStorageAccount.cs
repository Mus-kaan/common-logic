//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Rest.Azure;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal partial class LiftrAzure
    {
        #region Storage Account
        public async Task<IStorageAccount> GetOrCreateStorageAccountAsync(
            Region location,
            string rgName,
            string storageAccountName,
            IDictionary<string, string> tags,
            string accessFromSubnetId = null,
            CancellationToken cancellationToken = default)
        {
            var stor = await GetStorageAccountAsync(rgName, storageAccountName, cancellationToken);

            if (stor == null)
            {
                stor = await CreateStorageAccountAsync(location, rgName, storageAccountName, tags, accessFromSubnetId, cancellationToken);
            }

            return stor;
        }

        public async Task<IStorageAccount> CreateStorageAccountAsync(
            Region location,
            string rgName,
            string storageAccountName,
            IDictionary<string, string> tags,
            string accessFromSubnetId = null,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Creating storage account with name {storageAccountName} in {rgName}", storageAccountName, rgName);

            var storageAccountCreatable = FluentClient.StorageAccounts
                .Define(storageAccountName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithOnlyHttpsTraffic()
                .WithGeneralPurposeAccountKindV2()
                .WithTags(tags);

            if (!string.IsNullOrEmpty(accessFromSubnetId))
            {
                storageAccountCreatable = storageAccountCreatable
                    .WithAccessFromSelectedNetworks()
                    .WithAccessFromNetworkSubnet(accessFromSubnetId);
            }

            if (AvailabilityZoneRegionLookup.HasSupportStorage(location))
            {
                // GZRS & RAGZRS also have zone redundant but might violate "data resident" as the paired region might be out of GEO
                storageAccountCreatable = storageAccountCreatable
                    .WithSku(StorageAccountSkuType.Standard_ZRS);
            }

            var storageAccount = await storageAccountCreatable.CreateAsync(cancellationToken);

            _logger.Information("Created storage account with {resourceId}", storageAccount.Id);
            return storageAccount;
        }

        public async Task<IStorageAccount> GetStorageAccountAsync(
            string rgName,
            string storageAccountName,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Getting storage account. rgName: {rgName}, storageAccountName: {storageAccountName} ...", rgName, storageAccountName);
            var stor = await FluentClient
                .StorageAccounts
                .GetByResourceGroupAsync(rgName, storageAccountName, cancellationToken);

            if (stor == null)
            {
                _logger.Information("Cannot find storage account. rgName: {rgName}, storageAccountName: {storageAccountName} ...", rgName, storageAccountName);
            }

            return stor;
        }

        public async Task<IStorageAccount> FindStorageAccountAsync(
            string storageAccountName,
            string resourceGroupNamePrefix = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<IResourceGroup> resourceGroups = await FluentClient
                .ResourceGroups
                .ListAsync(loadAllPages: true, cancellationToken: cancellationToken);

            if (!string.IsNullOrEmpty(resourceGroupNamePrefix))
            {
                resourceGroups = resourceGroups.Where(rg => rg.Name.OrdinalStartsWith(resourceGroupNamePrefix));
            }

            if (resourceGroups?.Any() != true)
            {
                return null;
            }

            _logger.Information("Finding storage account with name '{storageAccountName}' in total {rgCount} resource groups", storageAccountName, resourceGroups.Count());

            foreach (var rg in resourceGroups)
            {
                var account = await FluentClient
                    .StorageAccounts
                    .GetByResourceGroupAsync(rg.Name, storageAccountName, cancellationToken);

                if (account != null)
                {
                    _logger.Information("Found storage account with Id {storageAccountId}", account.Id);
                    return account;
                }
            }

            return null;
        }

        public async Task<IEnumerable<IStorageAccount>> ListStorageAccountAsync(
            string rgName,
            string namePrefix = null,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Listing storage accounts in rgName '{rgName}' with prefix '{namePrefix}' ...", rgName, namePrefix);

            var accounts = await FluentClient
                .StorageAccounts
                .ListByResourceGroupAsync(rgName, loadAllPages: true, cancellationToken: cancellationToken);

            IEnumerable<IStorageAccount> filteredAccount = accounts.ToList();
            if (!string.IsNullOrEmpty(namePrefix))
            {
                filteredAccount = filteredAccount.Where((acct) => acct.Name.OrdinalStartsWith(namePrefix));
            }

            _logger.Information("Found {cnt} storage accounts in rgName '{rgName}' with prefix '{namePrefix}'.", filteredAccount.Count(), rgName, namePrefix);
            return filteredAccount;
        }

        public async Task GrantBlobContributorAsync(
            IResourceGroup rg,
            string objectId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageBlobDataContributorRoleDefinitionId())
                              .WithResourceGroupScope(rg)
                              .CreateAsync(cancellationToken);
                _logger.Information("Granted 'Storage Blob Data Contributor' of Resource Group '{rgId}' to SPN with object Id {objectId}. roleDefinitionId: {roleDefinitionId}", rg.Id, objectId, GetStorageBlobDataContributorRoleDefinitionId());
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
            catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
            {
                _logger.Error("The object Id '{objectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", objectId);
                throw;
            }
        }

        public Task GrantBlobContributorAsync(IResourceGroup rg, IIdentity msi, CancellationToken cancellationToken = default)
            => GrantBlobContributorAsync(rg, msi.GetObjectId(), cancellationToken);

        public async Task GrantBlobContributorAsync(IStorageAccount storageAccount, string objectId, CancellationToken cancellationToken = default)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageBlobDataContributorRoleDefinitionId())
                              .WithResourceScope(storageAccount)
                              .CreateAsync(cancellationToken);
                _logger.Information("Granted 'Storage Blob Data Contributor' of Storage account '{storageAccountId}' to SPN with object Id {objectId}. roleDefinitionId: {roleDefinitionId}", storageAccount.Id, objectId, GetStorageBlobDataContributorRoleDefinitionId());
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
            catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
            {
                _logger.Error("The object Id '{objectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", objectId);
                throw;
            }
        }

        public Task GrantBlobContributorAsync(string subscriptionId, IIdentity msi, CancellationToken cancellationToken = default)
            => GrantBlobContributorAsync(subscriptionId, msi.GetObjectId(), cancellationToken);

        public async Task GrantBlobContributorAsync(string subscriptionId, string objectId, CancellationToken cancellationToken = default)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageBlobDataContributorRoleDefinitionId())
                              .WithSubscriptionScope(subscriptionId)
                              .CreateAsync(cancellationToken);
                _logger.Information("Granted 'Storage Blob Data Contributor' of subscription '{subscriptionId}' to SPN with object Id {objectId}. roleDefinitionId: {roleDefinitionId}", subscriptionId, objectId, GetStorageBlobDataContributorRoleDefinitionId());
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
            catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
            {
                _logger.Error("The object Id '{objectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", objectId);
                throw;
            }
        }

        public Task GrantBlobContributorAsync(IStorageAccount storageAccount, IIdentity msi, CancellationToken cancellationToken = default)
           => GrantBlobContributorAsync(storageAccount, msi.GetObjectId(), cancellationToken);

        public async Task GrantBlobContainerContributorAsync(IStorageAccount storageAccount, string containerName, string objectId, CancellationToken cancellationToken = default)
        {
            try
            {
                var containerId = $"{storageAccount.Id}/blobServices/default/containers/{containerName}";
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageBlobDataContributorRoleDefinitionId())
                              .WithScope(containerId)
                              .CreateAsync(cancellationToken);
                _logger.Information("Granted 'Storage Blob Data Contributor' of blob container '{containerName}' to SPN with object Id {objectId}. roleDefinitionId: {roleDefinitionId}, containerId: {containerId}", containerName, objectId, GetStorageBlobDataContributorRoleDefinitionId(), containerId);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
            catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
            {
                _logger.Error("The object Id '{objectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", objectId);
                throw;
            }
        }

        public Task GrantBlobContainerContributorAsync(
            IStorageAccount storageAccount,
            string containerName,
            IIdentity msi,
            CancellationToken cancellationToken = default)
            => GrantBlobContainerContributorAsync(storageAccount, containerName, msi.GetObjectId(), cancellationToken);

        public async Task GrantBlobContainerReaderAsync(
            IStorageAccount storageAccount,
            string containerName,
            string objectId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerId = $"{storageAccount.Id}/blobServices/default/containers/{containerName}";
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageBlobDataReaderRoleDefinitionId())
                              .WithScope(containerId)
                              .CreateAsync(cancellationToken);
                _logger.Information("Granted 'Storage Blob Data Reader' of blob container '{containerName}' to SPN with object Id '{objectId}'. roleDefinitionId: {roleDefinitionId}, containerId: {containerId}", containerName, objectId, GetStorageBlobDataReaderRoleDefinitionId(), containerId);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
            catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
            {
                _logger.Error("The object Id '{objectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", objectId);
                throw;
            }
        }

        public Task GrantBlobContainerReaderAsync(
            IStorageAccount storageAccount,
            string containerName,
            IIdentity msi,
            CancellationToken cancellationToken = default)
            => GrantBlobContainerReaderAsync(storageAccount, containerName, msi.GetObjectId(), cancellationToken);

        public async Task GrantQueueContributorAsync(IResourceGroup rg, string objectId, CancellationToken cancellationToken = default)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageQueueDataContributorRoleDefinitionId())
                              .WithResourceGroupScope(rg)
                              .CreateAsync(cancellationToken);
                _logger.Information("Granted 'Storage Queue Data Contributor' of Resource Group '{rgId}' to SPN with object Id {objectId}. roleDefinitionId: {roleDefinitionId}", rg.Id, objectId, GetStorageQueueDataContributorRoleDefinitionId());
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
            catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
            {
                _logger.Error("The object Id '{objectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", objectId);
                throw;
            }
        }

        public Task GrantQueueContributorAsync(IResourceGroup rg, IIdentity msi, CancellationToken cancellationToken = default)
            => GrantQueueContributorAsync(rg, msi.GetObjectId(), cancellationToken);

        public Task GrantQueueContributorAsync(IStorageAccount storageAccount, IIdentity msi, CancellationToken cancellationToken = default)
            => GrantQueueContributorAsync(storageAccount, msi.GetObjectId(), cancellationToken);

        public async Task GrantQueueContributorAsync(IStorageAccount storageAccount, string objectId, CancellationToken cancellationToken = default)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageQueueDataContributorRoleDefinitionId())
                              .WithScope(storageAccount.Id)
                              .CreateAsync(cancellationToken);
                _logger.Information("Granted 'Storage Queue Data Contributor' storage account '{resourceId}' to SPN with object Id {objectId}. roleDefinitionId: {roleDefinitionId}", storageAccount.Id, objectId, GetStorageQueueDataContributorRoleDefinitionId());
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
            catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
            {
                _logger.Error("The object Id '{objectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", objectId);
                throw;
            }
        }

        public async Task DelegateStorageKeyOperationToKeyVaultAsync(IStorageAccount storageAccount, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Information("Assigning 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId} ...", GetStorageAccountKeyOperatorRoleDefinitionId(), _options.AzureKeyVaultObjectId);
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(_options.AzureKeyVaultObjectId)
                              .WithRoleDefinition(GetStorageAccountKeyOperatorRoleDefinitionId())
                              .WithScope(storageAccount.Id)
                              .CreateAsync(cancellationToken);
                _logger.Information("Granted 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId}", GetStorageAccountKeyOperatorRoleDefinitionId(), _options.AzureKeyVaultObjectId);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
        }

        public async Task DelegateStorageKeyOperationToKeyVaultAsync(IResourceGroup rg, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Information("Assigning 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId} on rg: {rgId} ...", GetStorageAccountKeyOperatorRoleDefinitionId(), _options.AzureKeyVaultObjectId, rg.Id);
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(_options.AzureKeyVaultObjectId)
                              .WithRoleDefinition(GetStorageAccountKeyOperatorRoleDefinitionId())
                              .WithResourceGroupScope(rg)
                              .CreateAsync(cancellationToken);
                _logger.Information("Granted 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId} on rg: {rgId}", GetStorageAccountKeyOperatorRoleDefinitionId(), _options.AzureKeyVaultObjectId, rg.Id);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
        }

        private string GetStorageBlobDataContributorRoleDefinitionId()
            => $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe"; // Storage Blob Data Contributor

        private string GetStorageBlobDataReaderRoleDefinitionId()
            => $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/2a2b9908-6ea1-4ae2-8e65-a410df84e7d1"; // Storage Blob Data Reader

        private string GetStorageQueueDataContributorRoleDefinitionId()
            => $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/974c5e8b-45b9-4653-ba55-5f855dd0fb88"; // Storage Queue Data Contributor

        private string GetStorageAccountKeyOperatorRoleDefinitionId()
            => $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/81a9662b-bebf-436f-a333-f67b29880f12"; // Storage Account Key Operator Service Role
        #endregion Storage Account
    }
}
