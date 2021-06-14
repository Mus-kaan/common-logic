//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Eventhub.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.Monitor.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.Redis.Fluent;
using Microsoft.Azure.Management.Redis.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Contracts.AzureMonitor;
using Microsoft.Rest.Azure;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.Management.Fluent.Azure;
using TimeSpan = System.TimeSpan;

namespace Microsoft.Liftr.Fluent
{
    /// <summary>
    /// This is not thread safe, since IAzure is not thread safe by design.
    /// Please use 'LiftrAzureFactory' to dynamiclly generate it.
    /// Please do not add 'LiftrAzure' to the dependency injection container, use 'LiftrAzureFactory' instead.
    /// </summary>
    internal class LiftrAzure : ILiftrAzure
    {
        public const string c_vnetAddressSpace = "10.66.0.0/16";                // 10.66.0.0 - 10.66.255.255 (65536 addresses)
        public const string c_defaultSubnetAddressSpace = "10.66.255.0/24";     // 10.66.255.0 - 10.66.255.255 (256 addresses)
        public const string c_AspEnv = "ASPNETCORE_ENVIRONMENT";
        private readonly LiftrAzureOptions _options;
        private readonly ILogger _logger;

        public LiftrAzure(
            string tenantId,
            string defaultSubscriptionId,
            string spnObjectId,
            TokenCredential tokenCredential,
            AzureCredentials credentials,
            IAzure fluentClient,
            IAuthenticated authenticated,
            LiftrAzureOptions options,
            ILogger logger)
        {
            TenantId = tenantId;
            DefaultSubscriptionId = defaultSubscriptionId;
            SPNObjectId = spnObjectId;
            TokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
            AzureCredentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            FluentClient = fluentClient ?? throw new ArgumentNullException(nameof(fluentClient));
            Authenticated = authenticated ?? throw new ArgumentNullException(nameof(authenticated));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string TenantId { get; }

        public string DefaultSubscriptionId { get; }

        public string SPNObjectId { get; }

        public string DefaultSubnetName { get; } = "default";

        public IAzure FluentClient { get; }

        public IAuthenticated Authenticated { get; }

        public TokenCredential TokenCredential { get; }

        public AzureCredentials AzureCredentials { get; }

        public LiftrAzureOptions Options => _options;

        public async Task<string> GetResourceAsync(string resourceId, string apiVersion)
        {
            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                if (string.IsNullOrEmpty(resourceId))
                {
                    throw new ArgumentNullException(nameof(resourceId));
                }

                if (string.IsNullOrEmpty(apiVersion))
                {
                    throw new ArgumentNullException(nameof(apiVersion));
                }

                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = resourceId;
                uriBuilder.Query = $"api-version={apiVersion}";
                _logger.Information($"Start getting resource at Uri: {uriBuilder.Uri}");
                var runOutputResponse = await _options.HttpPolicy.ExecuteAsync(() => httpClient.GetAsync(uriBuilder.Uri));

                if (runOutputResponse.StatusCode == HttpStatusCode.OK)
                {
                    return await runOutputResponse.Content.ReadAsStringAsync();
                }
                else if (runOutputResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    var errMsg = $"Failed at getting resource with Id '{resourceId}'. statusCode: '{runOutputResponse.StatusCode}'";
                    if (runOutputResponse?.Content != null)
                    {
                        errMsg = errMsg + $", response: {await runOutputResponse.Content?.ReadAsStringAsync()}";
                    }

                    var ex = new InvalidOperationException(errMsg);
                    _logger.Error(ex.Message);
                    throw ex;
                }
            }
        }

        public async Task PutResourceAsync(string resourceId, string apiVersion, string resourceJsonBody, CancellationToken cancellationToken = default)
        {
            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                if (string.IsNullOrEmpty(resourceId))
                {
                    throw new ArgumentNullException(nameof(resourceId));
                }

                if (string.IsNullOrEmpty(apiVersion))
                {
                    throw new ArgumentNullException(nameof(apiVersion));
                }

                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = resourceId;
                uriBuilder.Query = $"api-version={apiVersion}";

                _logger.Information($"Start putting resource at Uri: {uriBuilder.Uri}");
                using var httpContent = new StringContent(resourceJsonBody, Encoding.UTF8, "application/json");
                var runOutputResponse = await _options.HttpPolicy.ExecuteAsync(() => httpClient.PutAsync(uriBuilder.Uri, httpContent));

                if (!runOutputResponse.IsSuccessStatusCode)
                {
                    var errMsg = $"Failed at putting resource with Id '{resourceId}'. statusCode: '{runOutputResponse.StatusCode}'";
                    if (runOutputResponse?.Content != null)
                    {
                        errMsg = errMsg + $", response: {await runOutputResponse.Content?.ReadAsStringAsync()}";
                    }

                    var ex = new InvalidOperationException(errMsg);
                    _logger.Error(ex.Message);
                    throw ex;
                }
                else if (runOutputResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    await WaitAsyncOperationAsync(httpClient, runOutputResponse, cancellationToken);
                }

                _logger.Information($"Finished putting resource at Uri: {uriBuilder.Uri}");
            }
        }

        public async Task PatchResourceAsync(string resourceId, string apiVersion, string resourceJsonBody, CancellationToken cancellationToken = default)
        {
            using var handler = new AzureApiAuthHandler(AzureCredentials);
            using var httpClient = new HttpClient(handler);
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrEmpty(apiVersion))
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
            uriBuilder.Path = resourceId;
            uriBuilder.Query = $"api-version={apiVersion}";

            _logger.Information($"Start PATCH resource at Uri: {uriBuilder.Uri}");
            using var httpContent = new StringContent(resourceJsonBody, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(new HttpMethod("PATCH"), uriBuilder.Uri)
            {
                Content = httpContent,
            };
            var runOutputResponse = await _options.HttpPolicy.ExecuteAsync(() => httpClient.SendAsync(request));

            if (!runOutputResponse.IsSuccessStatusCode)
            {
                var errMsg = $"Failed at patching resource with Id '{resourceId}'. statusCode: '{runOutputResponse.StatusCode}'";
                if (runOutputResponse?.Content != null)
                {
                    errMsg = errMsg + $", response: {await runOutputResponse.Content?.ReadAsStringAsync()}";
                }

                var ex = new InvalidOperationException(errMsg);
                _logger.Error(ex.Message);
                throw ex;
            }
            else if (runOutputResponse.StatusCode == HttpStatusCode.Accepted)
            {
                await WaitAsyncOperationAsync(httpClient, runOutputResponse, cancellationToken);
            }

            _logger.Information($"Finished PATCH resource at Uri: {uriBuilder.Uri}");
        }

        public async Task DeleteResourceAsync(string resourceId, string apiVersion, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrEmpty(apiVersion))
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            // https://github.com/Azure/azure-rest-api-specs/blob/master/specification/imagebuilder/resource-manager/Microsoft.VirtualMachineImages/stable/2020-02-14/imagebuilder.json#L280
            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = resourceId;
                uriBuilder.Query = $"api-version={apiVersion}";
                _logger.Information($"Start deleting resource at Uri: {uriBuilder.Uri}");
                var deleteResponse = await _options.HttpPolicy.ExecuteAsync((ct) => httpClient.DeleteAsync(uriBuilder.Uri, ct), cancellationToken);
                _logger.Information($"DELETE response code: {deleteResponse.StatusCode}");

                if (!deleteResponse.IsSuccessStatusCode)
                {
                    _logger.Error($"Deleting resource at Uri: '{uriBuilder.Uri}' failed with error code '{deleteResponse.StatusCode}'");
                    if (deleteResponse?.Content != null)
                    {
                        var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                        _logger.Error("Error response body: {errorContent}", errorContent);
                    }

                    throw new InvalidOperationException($"Delete resource with id '{resourceId}' failed.");
                }
                else if (deleteResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    await WaitAsyncOperationAsync(httpClient, deleteResponse, cancellationToken);
                }

                _logger.Information($"Finished deleting resource at Uri: {uriBuilder.Uri}");
                return;
            }
        }

        #region Resource provider
        public Task<string> RegisterFeatureAsync(string resourceProviderName, string featureName)
        {
            return RegisterFeatureAsync(FluentClient.SubscriptionId, resourceProviderName, featureName);
        }

        public async Task<string> RegisterFeatureAsync(string subscriptionId, string resourceProviderName, string featureName)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            if (string.IsNullOrEmpty(resourceProviderName))
            {
                throw new ArgumentNullException(nameof(resourceProviderName));
            }

            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = $"/subscriptions/{subscriptionId}/providers/Microsoft.Features/providers/{resourceProviderName}/features/{featureName}/register";
                uriBuilder.Query = $"api-version=2015-12-01";

                _logger.Debug($"Start registering resource provider '{resourceProviderName}' in subscription '{subscriptionId}'");
                var response = await _options.HttpPolicy.ExecuteAsync(() => httpClient.PostAsync(uriBuilder.Uri, null));

                if (!response.IsSuccessStatusCode)
                {
                    var errMsg = $"Failed at registering resource provider. Status code: {response.Content}";

                    if (response.Content != null)
                    {
                        errMsg += $"Error content: {await response.Content.ReadAsStringAsync()}";
                    }

                    var ex = new InvalidOperationException(errMsg);
                    _logger.Error(ex, errMsg);
                    throw ex;
                }

                return await response.Content.ReadAsStringAsync();
            }
        }

        public Task<string> RegisterResourceProviderAsync(string resourceProviderName)
        {
            return RegisterResourceProviderAsync(FluentClient.SubscriptionId, resourceProviderName);
        }

        public async Task<string> RegisterResourceProviderAsync(string subscriptionId, string resourceProviderName)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            if (string.IsNullOrEmpty(resourceProviderName))
            {
                throw new ArgumentNullException(nameof(resourceProviderName));
            }

            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = $"/subscriptions/{subscriptionId}/providers/{resourceProviderName}/register";
                uriBuilder.Query = $"api-version=2014-04-01-preview";

                _logger.Information($"Start registering resource provider '{resourceProviderName}' in subscription '{subscriptionId}'");
                var response = await _options.HttpPolicy.ExecuteAsync(() => httpClient.PostAsync(uriBuilder.Uri, null));

                if (!response.IsSuccessStatusCode)
                {
                    var errMsg = $"Failed at registering resource provider. Status code: {response.Content}";

                    if (response.Content != null)
                    {
                        errMsg += $"Error content: {await response.Content.ReadAsStringAsync()}";
                    }

                    var ex = new InvalidOperationException(errMsg);
                    _logger.Error(ex, errMsg);
                    throw ex;
                }

                return await response.Content.ReadAsStringAsync();
            }
        }
        #endregion

        #region Resource Group
        public async Task<IResourceGroup> GetOrCreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags)
        {
            var rg = await GetResourceGroupAsync(rgName);

            if (rg == null)
            {
                rg = await CreateResourceGroupAsync(location, rgName, tags);
            }

            return rg;
        }

        public async Task<IResourceGroup> CreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags)
        {
            _logger.Information("Creating a resource group with name: {rgName}", rgName);
            var rg = await FluentClient
                .ResourceGroups
                .Define(rgName)
                .WithRegion(location)
                .WithTags(tags)
                .CreateAsync();
            _logger.Information("Created a resource group with Id:{resourceId}", rg.Id);
            return rg;
        }

        public IResourceGroup CreateResourceGroup(Region location, string rgName, IDictionary<string, string> tags)
        {
            _logger.Information("Creating a resource group with name: {rgName}", rgName);
            var rg = FluentClient
                .ResourceGroups
                .Define(rgName)
                .WithRegion(location)
                .WithTags(tags)
                .Create();
            _logger.Information("Created a resource group with Id:{resourceId}", rg.Id);
            return rg;
        }

        public async Task<IResourceGroup> GetResourceGroupAsync(string rgName)
        {
            try
            {
                _logger.Information("Getting resource group with name: {rgName}", rgName);
                var rg = await FluentClient
                .ResourceGroups
                .GetByNameAsync(rgName);
                if (rg != null)
                {
                    _logger.Information("Retrieved the Resource Group with Id: {resourceId}", rg.Id);
                    return rg;
                }
            }
            catch (CloudException ex) when (ex.IsNotFound())
            {
            }

            return null;
        }

        public async Task DeleteResourceGroupAsync(string rgName, bool noThrow = false)
        {
            _logger.Information("Deleteing resource group with name: " + rgName);
            try
            {
                await FluentClient
                .ResourceGroups
                .DeleteByNameAsync(rgName);
                _logger.Information("Finished delete resource group with name: " + rgName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Cannot delete resource group with name {rgName}", rgName);
                if (!noThrow)
                {
                    throw;
                }
            }
        }

        public void DeleteResourceGroup(string rgName, bool noThrow = false)
        {
            _logger.Information("Deleteing resource group with name: " + rgName);
            try
            {
                FluentClient
                .ResourceGroups
                .DeleteByName(rgName);
                _logger.Information("Finished delete resource group with name: " + rgName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Cannot delete resource group with name {rgName}", rgName);
                if (!noThrow)
                {
                    throw;
                }
            }
        }

        public async Task DeleteResourceGroupWithTagAsync(string tagName, string tagValue, Func<IReadOnlyDictionary<string, string>, bool> tagsFilter = null)
        {
            _logger.Information($"Listing resource groups in subscription: {FluentClient.SubscriptionId}");
            var rgs = await FluentClient
                .ResourceGroups
                .ListByTagAsync(tagName, tagValue);
            _logger.Information("There are {@rgCount} with tagName {@tagName} and {@tagValue}.", rgs.Count(), tagName, tagValue);

            List<Task> tasks = new List<Task>();
            foreach (var rg in rgs)
            {
                if (tagsFilter == null || tagsFilter.Invoke(rg.Tags) == true)
                {
                    tasks.Add(DeleteResourceGroupAsync(rg.Name, noThrow: true));
                }
            }

            await Task.WhenAll(tasks);
        }

        public async Task DeleteResourceGroupWithPrefixAsync(string rgNamePrefix)
        {
            _logger.Information($"Listing resource groups in subscription: {FluentClient.SubscriptionId}");
            var rgs = await FluentClient
                .ResourceGroups
                .ListAsync();

            var toDelete = rgs.Where(rg => rg.Name.OrdinalStartsWith(rgNamePrefix));

            _logger.Information("There are {toDeletCount} resource groups with prefix {rgPrefix} in total {rgCount}.", toDelete.Count(), rgNamePrefix, rgs.Count());

            List<Task> tasks = new List<Task>();
            foreach (var rg in toDelete)
            {
                tasks.Add(DeleteResourceGroupAsync(rg.Name, noThrow: true));
            }

            await Task.WhenAll(tasks);
        }

        public async Task DeleteResourceGroupWithNamePartAsync(string rgNamePart)
        {
            _logger.Information($"Listing resource groups in subscription: {FluentClient.SubscriptionId}");
            var rgs = await FluentClient
                .ResourceGroups
                .ListAsync();

            var toDelete = rgs.Where(rg => rg.Name.OrdinalContains(rgNamePart));

            _logger.Information("There are {toDeletCount} resource groups with name part {rgPrefix} in total {rgCount}.", toDelete.Count(), rgNamePart, rgs.Count());

            List<Task> tasks = new List<Task>();
            foreach (var rg in toDelete)
            {
                tasks.Add(DeleteResourceGroupAsync(rg.Name, noThrow: true));
            }

            await Task.WhenAll(tasks);
        }
        #endregion Resource Group

        #region Storage Account
        public async Task<IStorageAccount> GetOrCreateStorageAccountAsync(Region location, string rgName, string storageAccountName, IDictionary<string, string> tags, string accessFromSubnetId = null)
        {
            var stor = await GetStorageAccountAsync(rgName, storageAccountName);

            if (stor == null)
            {
                stor = await CreateStorageAccountAsync(location, rgName, storageAccountName, tags, accessFromSubnetId);
            }

            return stor;
        }

        public async Task<IStorageAccount> CreateStorageAccountAsync(Region location, string rgName, string storageAccountName, IDictionary<string, string> tags, string accessFromSubnetId = null)
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

            var storageAccount = await storageAccountCreatable.CreateAsync();

            _logger.Information("Created storage account with {resourceId}", storageAccount.Id);
            return storageAccount;
        }

        public async Task<IStorageAccount> GetStorageAccountAsync(string rgName, string storageAccountName)
        {
            _logger.Information("Getting storage account. rgName: {rgName}, storageAccountName: {storageAccountName} ...", rgName, storageAccountName);
            var stor = await FluentClient
                .StorageAccounts
                .GetByResourceGroupAsync(rgName, storageAccountName);

            if (stor == null)
            {
                _logger.Information("Cannot find storage account. rgName: {rgName}, storageAccountName: {storageAccountName} ...", rgName, storageAccountName);
            }

            return stor;
        }

        public async Task<IStorageAccount> FindStorageAccountAsync(string storageAccountName, string resourceGroupNamePrefix = null)
        {
            IEnumerable<IResourceGroup> resourceGroups = await FluentClient
                .ResourceGroups
                .ListAsync();

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
                    .GetByResourceGroupAsync(rg.Name, storageAccountName);

                if (account != null)
                {
                    _logger.Information("Found storage account with Id {storageAccountId}", account.Id);
                    return account;
                }
            }

            return null;
        }

        public async Task<IEnumerable<IStorageAccount>> ListStorageAccountAsync(string rgName, string namePrefix = null)
        {
            _logger.Information("Listing storage accounts in rgName '{rgName}' with prefix '{namePrefix}' ...", rgName, namePrefix);

            var accounts = await FluentClient
                .StorageAccounts
                .ListByResourceGroupAsync(rgName);

            IEnumerable<IStorageAccount> filteredAccount = accounts.ToList();
            if (!string.IsNullOrEmpty(namePrefix))
            {
                filteredAccount = filteredAccount.Where((acct) => acct.Name.OrdinalStartsWith(namePrefix));
            }

            _logger.Information("Found {cnt} storage accounts in rgName '{rgName}' with prefix '{namePrefix}'.", filteredAccount.Count(), rgName, namePrefix);
            return filteredAccount;
        }

        public async Task GrantBlobContributorAsync(IResourceGroup rg, string objectId)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageBlobDataContributorRoleDefinitionId())
                              .WithResourceGroupScope(rg)
                              .CreateAsync();
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

        public Task GrantBlobContributorAsync(IResourceGroup rg, IIdentity msi)
            => GrantBlobContributorAsync(rg, msi.GetObjectId());

        public async Task GrantBlobContributorAsync(IStorageAccount storageAccount, string objectId)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageBlobDataContributorRoleDefinitionId())
                              .WithResourceScope(storageAccount)
                              .CreateAsync();
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

        public Task GrantBlobContributorAsync(string subscriptionId, IIdentity msi)
            => GrantBlobContributorAsync(subscriptionId, msi.GetObjectId());

        public async Task GrantBlobContributorAsync(string subscriptionId, string objectId)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageBlobDataContributorRoleDefinitionId())
                              .WithSubscriptionScope(subscriptionId)
                              .CreateAsync();
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

        public Task GrantBlobContributorAsync(IStorageAccount storageAccount, IIdentity msi)
           => GrantBlobContributorAsync(storageAccount, msi.GetObjectId());

        public async Task GrantBlobContainerContributorAsync(IStorageAccount storageAccount, string containerName, string objectId)
        {
            try
            {
                var containerId = $"{storageAccount.Id}/blobServices/default/containers/{containerName}";
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageBlobDataContributorRoleDefinitionId())
                              .WithScope(containerId)
                              .CreateAsync();
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

        public Task GrantBlobContainerContributorAsync(IStorageAccount storageAccount, string containerName, IIdentity msi)
            => GrantBlobContainerContributorAsync(storageAccount, containerName, msi.GetObjectId());

        public async Task GrantBlobContainerReaderAsync(IStorageAccount storageAccount, string containerName, string objectId)
        {
            try
            {
                var containerId = $"{storageAccount.Id}/blobServices/default/containers/{containerName}";
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageBlobDataReaderRoleDefinitionId())
                              .WithScope(containerId)
                              .CreateAsync();
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

        public Task GrantBlobContainerReaderAsync(IStorageAccount storageAccount, string containerName, IIdentity msi)
            => GrantBlobContainerReaderAsync(storageAccount, containerName, msi.GetObjectId());

        public async Task GrantQueueContributorAsync(IResourceGroup rg, string objectId)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageQueueDataContributorRoleDefinitionId())
                              .WithResourceGroupScope(rg)
                              .CreateAsync();
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

        public Task GrantQueueContributorAsync(IResourceGroup rg, IIdentity msi)
            => GrantQueueContributorAsync(rg, msi.GetObjectId());

        public Task GrantQueueContributorAsync(IStorageAccount storageAccount, IIdentity msi)
            => GrantQueueContributorAsync(storageAccount, msi.GetObjectId());

        public async Task GrantQueueContributorAsync(IStorageAccount storageAccount, string objectId)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(GetStorageQueueDataContributorRoleDefinitionId())
                              .WithScope(storageAccount.Id)
                              .CreateAsync();
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

        public async Task DelegateStorageKeyOperationToKeyVaultAsync(IStorageAccount storageAccount)
        {
            try
            {
                _logger.Information("Assigning 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId} ...", GetStorageAccountKeyOperatorRoleDefinitionId(), _options.AzureKeyVaultObjectId);
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(_options.AzureKeyVaultObjectId)
                              .WithRoleDefinition(GetStorageAccountKeyOperatorRoleDefinitionId())
                              .WithScope(storageAccount.Id)
                              .CreateAsync();
                _logger.Information("Granted 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId}", GetStorageAccountKeyOperatorRoleDefinitionId(), _options.AzureKeyVaultObjectId);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
        }

        public async Task DelegateStorageKeyOperationToKeyVaultAsync(IResourceGroup rg)
        {
            try
            {
                _logger.Information("Assigning 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId} on rg: {rgId} ...", GetStorageAccountKeyOperatorRoleDefinitionId(), _options.AzureKeyVaultObjectId, rg.Id);
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(_options.AzureKeyVaultObjectId)
                              .WithRoleDefinition(GetStorageAccountKeyOperatorRoleDefinitionId())
                              .WithResourceGroupScope(rg)
                              .CreateAsync();
                _logger.Information("Granted 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId} on rg: {rgId}", GetStorageAccountKeyOperatorRoleDefinitionId(), _options.AzureKeyVaultObjectId, rg.Id);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
        }
        #endregion Storage Account

        #region Network
        public async Task<INetworkSecurityGroup> GetOrCreateDefaultNSGAsync(Region location, string rgName, string nsgName, IDictionary<string, string> tags)
        {
            var nsg = await GetNSGAsync(rgName, nsgName);

            if (nsg != null)
            {
                _logger.Information("Using existing nsg with id '{nsgId}'.", nsg.Id);
                return nsg;
            }

            nsg = await FluentClient.NetworkSecurityGroups
                .Define(nsgName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .AllowAny80TCPInBound()
                .AllowAny443TCPInBound()
                .WithTags(tags)
                .CreateAsync();

            _logger.Information("Created default nsg with id '{nsgId}'.", nsg.Id);
            return nsg;
        }

        public Task<INetworkSecurityGroup> GetNSGAsync(string rgName, string nsgName)
        {
            return FluentClient.NetworkSecurityGroups
                .GetByResourceGroupAsync(rgName, nsgName);
        }

        public async Task<INetwork> GetOrCreateVNetAsync(Region location, string rgName, string vnetName, IDictionary<string, string> tags, string nsgId = null)
        {
            var vnet = await GetVNetAsync(rgName, vnetName);

            if (vnet == null)
            {
                vnet = await CreateVNetAsync(location, rgName, vnetName, tags, nsgId);
            }

            return vnet;
        }

        public async Task<INetwork> GetVNetAsync(string rgName, string vnetName)
        {
            _logger.Information("Getting VNet. rgName: {rgName}, vnetName: {vnetName} ...", rgName, vnetName);
            var vnet = await FluentClient
                .Networks
                .GetByResourceGroupAsync(rgName, vnetName);

            if (vnet == null)
            {
                _logger.Information("Cannot find VNet. rgName: {rgName}, vnetName: {vnetName} ...", rgName, vnetName);
            }

            return vnet;
        }

        public async Task<INetwork> CreateVNetAsync(Region location, string rgName, string vnetName, IDictionary<string, string> tags, string nsgId = null)
        {
            _logger.Information("Creating vnet with name {vnetName} in {rgName}", vnetName, rgName);

            var temp = FluentClient.Networks
                .Define(vnetName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithAddressSpace(c_vnetAddressSpace)
                .DefineSubnet(DefaultSubnetName)
                .WithAddressPrefix(c_defaultSubnetAddressSpace)
                .WithAccessFromService(ServiceEndpointType.MicrosoftStorage)
                .WithAccessFromService(ServiceEndpointType.MicrosoftAzureCosmosDB)
                .WithAccessFromService(LiftrServiceEndpointType.MicrosoftKeyVault);

            if (!string.IsNullOrEmpty(nsgId))
            {
                temp = temp.WithExistingNetworkSecurityGroup(nsgId);
            }

            var vnet = await temp.Attach().WithTags(tags).CreateAsync();

            _logger.Information("Created VNet with {resourceId}", vnet.Id);
            return vnet;
        }

        public async Task<ISubnet> CreateNewSubnetAsync(INetwork vnet, string subnetName, string nsgId = null)
        {
            if (vnet == null)
            {
                return null;
            }

            var existingSubnets = vnet.Subnets;
            if (existingSubnets == null || existingSubnets.Count == 0)
            {
                var ex = new InvalidOperationException($"To create a new subnet similar to the existing subnets, please make sure there is at least one subnet in the existing vnet '{vnet.Id}'.");
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            _logger.Information("There exist {subnetCount} subnets in the vnet '{vnetId}'.", existingSubnets.Count(), vnet.Id);
            if (existingSubnets.ContainsKey(subnetName))
            {
                return existingSubnets[subnetName];
            }

            var oneSubnetPrefix = existingSubnets.FirstOrDefault().Value.AddressPrefix;
            var nonDefaultSubnets = existingSubnets.Where(kvp => !kvp.Key.OrdinalEquals(DefaultSubnetName)).Select(kvp => kvp.Value);
            var largestValue = nonDefaultSubnets
                .Select(subnet => int.Parse(subnet.AddressPrefix.Split('.')[2], CultureInfo.InvariantCulture))
                .OrderByDescending(i => i)
                .FirstOrDefault();

            var newIPPart = largestValue + 1;
            var parts = oneSubnetPrefix.Split('.');
            parts[2] = newIPPart.ToString(CultureInfo.InvariantCulture);
            var newCIDR = string.Join(".", parts);

            _logger.Information("Adding a new subnet with name {subnetName} and CIDR {subnetCIDR} to vnet '{vnetId}'.", subnetName, newCIDR, vnet.Id);

            var temp = vnet.Update()
                .DefineSubnet(subnetName)
                .WithAddressPrefix(newCIDR)
                .WithAccessFromService(ServiceEndpointType.MicrosoftStorage)
                .WithAccessFromService(ServiceEndpointType.MicrosoftAzureCosmosDB)
                .WithAccessFromService(LiftrServiceEndpointType.MicrosoftKeyVault);

            if (!string.IsNullOrEmpty(nsgId))
            {
                temp = temp.WithExistingNetworkSecurityGroup(nsgId);
            }

            await temp.Attach().ApplyAsync();
            await vnet.RefreshAsync();
            return vnet.Subnets[subnetName];
        }

        public async Task<ISubnet> GetSubnetAsync(string subnetId)
        {
            var parsedSubnetId = new Liftr.Contracts.ResourceId(subnetId);
            var vnet = await GetVNetAsync(parsedSubnetId.ResourceGroup, parsedSubnetId.ResourceName);
            if (vnet == null)
            {
                return null;
            }

            if (vnet.Subnets.ContainsKey(parsedSubnetId.ChildResourceName))
            {
                return vnet.Subnets[parsedSubnetId.ChildResourceName];
            }

            return null;
        }

        public async Task<IPublicIPAddress> GetOrCreatePublicIPAsync(Region location, string rgName, string pipName, IDictionary<string, string> tags, PublicIPSkuType skuType = null)
        {
            var pip = await GetPublicIPAsync(rgName, pipName);
            if (pip == null)
            {
                pip = await CreatePublicIPAsync(location, rgName, pipName, tags, skuType);
            }

            return pip;
        }

        public async Task<IPublicIPAddress> CreatePublicIPAsync(Region location, string rgName, string pipName, IDictionary<string, string> tags, PublicIPSkuType skuType = null)
        {
            if (skuType == null)
            {
                skuType = PublicIPSkuType.Basic;
            }

            _logger.Information("Start creating Public IP address with SKU: {skuType} with name: {pipName} in RG: {rgName} ...", skuType, pipName, rgName);

            var pip = await FluentClient
                .PublicIPAddresses
                .Define(pipName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithSku(skuType)
                .WithStaticIP()
                .WithLeafDomainLabel(pipName)
                .WithTags(tags)
                .CreateAsync();

            _logger.Information("Created Public IP address with resourceId: {resourceId}", pip.Id);
            return pip;
        }

        public Task<IPublicIPAddress> GetPublicIPAsync(string rgName, string pipName)
        {
            _logger.Information("Start getting Public IP with name: {pipName} ...", pipName);

            return FluentClient
                .PublicIPAddresses
                .GetByResourceGroupAsync(rgName, pipName);
        }

        public async Task<IEnumerable<IPublicIPAddress>> ListPublicIPAsync(string rgName, string namePrefix = null)
        {
            _logger.Information($"Listing Public IP in resource group {rgName} with prefix {namePrefix} ...");

            IEnumerable<IPublicIPAddress> ips = (await FluentClient
                .PublicIPAddresses
                .ListByResourceGroupAsync(rgName)).ToList();

            if (!string.IsNullOrEmpty(namePrefix))
            {
                ips = ips.Where((pip) => pip.Name.OrdinalStartsWith(namePrefix));
            }

            _logger.Information($"Found {ips.Count()} Public IP in resource group {rgName} with prefix {namePrefix}.");

            return ips;
        }

        public async Task<ITrafficManagerProfile> GetOrCreateTrafficManagerAsync(string rgName, string tmName, IDictionary<string, string> tags)
        {
            var tm = await GetTrafficManagerAsync(rgName, tmName);
            if (tm == null)
            {
                tm = await CreateTrafficManagerAsync(rgName, tmName, tags);
            }

            return tm;
        }

        public async Task<ITrafficManagerProfile> CreateTrafficManagerAsync(string rgName, string tmName, IDictionary<string, string> tags)
        {
            _logger.Information("Creating a Traffic Manager with name {@tmName} ...", tmName);
            var tm = await FluentClient
                .TrafficManagerProfiles
                .Define(tmName)
                .WithExistingResourceGroup(rgName)
                .WithLeafDomainLabel(tmName)
                .WithWeightBasedRouting()
                .DefineExternalTargetEndpoint("default-endpoint")
                    .ToFqdn("40.76.4.15") // microsoft.com
                    .FromRegion(Region.USWest)
                    .WithTrafficDisabled()
                    .Attach()
                .WithHttpsMonitoring(443, "/api/liveness-probe")
                .WithTags(tags)
                .CreateAsync();

            _logger.Information("Created Traffic Manager with Id {resourceId}", tm.Id);

            return tm;
        }

        public async Task<ITrafficManagerProfile> GetTrafficManagerAsync(string tmId)
        {
            _logger.Information("Getting a Traffic Manager with Id {resourceId} ...", tmId);
            var tm = await FluentClient
                .TrafficManagerProfiles
                .GetByIdAsync(tmId);

            return tm;
        }

        public Task<ITrafficManagerProfile> GetTrafficManagerAsync(string rgName, string tmName)
        {
            _logger.Information("Start getting Traffic Manager with name: {tmName} in RG {rgName} ...", tmName, rgName);
            return FluentClient
                .TrafficManagerProfiles
                .GetByResourceGroupAsync(rgName, tmName);
        }

        public Task<IDnsZone> GetDNSZoneAsync(string rgName, string dnsName)
        {
            _logger.Information("Getting DNS zone with dnsName '{dnsName}' in RG '{resourceGroup}'.", dnsName, rgName);
            return FluentClient
                .DnsZones
                .GetByResourceGroupAsync(rgName, dnsName);
        }

        public async Task<IDnsZone> CreateDNSZoneAsync(string rgName, string dnsName, IDictionary<string, string> tags)
        {
            var dns = await FluentClient
                .DnsZones
                .Define(dnsName)
                .WithExistingResourceGroup(rgName)
                .WithTags(tags)
                .CreateAsync();

            _logger.Information("Created DNS zone with id '{resourceId}'.", dns.Id);
            return dns;
        }

        #endregion

        #region CosmosDB
        public async Task<(ICosmosDBAccount cosmosDBAccount, string mongoConnectionString)> CreateCosmosDBAsync(
            Region location,
            string rgName,
            string cosmosDBName,
            IDictionary<string, string> tags,
            ISubnet subnet = null)
        {
            var cosmosDBAccount = await GetCosmosDBAsync(rgName, cosmosDBName);
            if (cosmosDBAccount == null)
            {
                var helper = new CosmosDBHelper(_logger);
                cosmosDBAccount = await helper.CreateCosmosDBAsync(this, location, rgName, cosmosDBName, tags);

                if (subnet != null)
                {
                    cosmosDBAccount = await cosmosDBAccount.Update().WithVirtualNetworkRule(subnet.Parent.Id, subnet.Name).ApplyAsync();
                }

                _logger.Information($"Created CosmosDB with name {cosmosDBName}");
            }

            _logger.Information("Get the MongoDB connection string");
            var databaseAccountListConnectionStringsResult = await cosmosDBAccount.ListConnectionStringsAsync();
            var mongoConnectionString = databaseAccountListConnectionStringsResult.ConnectionStrings[0].ConnectionString;

            return (cosmosDBAccount, mongoConnectionString);
        }

        public Task<ICosmosDBAccount> GetCosmosDBAsync(string dbResourceId)
        {
            _logger.Information("Getting CosmosDB with id '{dbResourceId}' ...", dbResourceId);
            return FluentClient
                .CosmosDBAccounts
                .GetByIdAsync(dbResourceId);
        }

        public Task<ICosmosDBAccount> GetCosmosDBAsync(string rgName, string cosmosDBName)
        {
            _logger.Information("Getting CosmosDB in rg '{rgName}' with name '{cosmosDBName}' ...", rgName, cosmosDBName);
            return FluentClient
                .CosmosDBAccounts
                .GetByResourceGroupAsync(rgName, cosmosDBName);
        }

        public async Task<IEnumerable<ICosmosDBAccount>> ListCosmosDBAsync(string rgName)
        {
            _logger.Information($"Listing CosmosDB in resource group {rgName} ...");
            return await FluentClient
                .CosmosDBAccounts
                .ListByResourceGroupAsync(rgName);
        }
        #endregion CosmosDB

        #region Key Vault
        public async Task<IVault> GetOrCreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags)
        {
            var kv = await GetKeyVaultAsync(rgName, vaultName);

            if (kv == null)
            {
                kv = await CreateKeyVaultAsync(location, rgName, vaultName, tags);
            }

            return kv;
        }

        public async Task<IVault> GetOrCreateKeyVaultAsync(Region location, string rgName, string vaultName, string accessibleFromIP, IDictionary<string, string> tags)
        {
            var kv = await GetKeyVaultAsync(rgName, vaultName);

            if (kv == null)
            {
                var helper = new KeyVaultHelper(_logger);
                kv = await helper.CreateKeyVaultAsync(this, location, rgName, vaultName, accessibleFromIP, tags);
            }

            return kv;
        }

        public async Task WithKeyVaultAccessFromNetworkAsync(IVault vault, string ipAddress, string subnetId, bool enableVNetFilter = true)
        {
            if (vault == null)
            {
                throw new ArgumentNullException(nameof(vault));
            }

            if (!enableVNetFilter && vault?.Inner?.Properties?.NetworkAcls?.DefaultAction != NetworkRuleAction.Deny)
            {
                _logger.Information("Skip adding VNet since Network isolation is not enabled for key vault {kvId}", vault.Id);
                return;
            }

            var helper = new KeyVaultHelper(_logger);
            await helper.WithAccessFromNetworkAsync(vault, this, ipAddress, subnetId);
        }

        public async Task<IVault> CreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags)
        {
            var helper = new KeyVaultHelper(_logger);
            var kv = await helper.CreateKeyVaultAsync(this, location, rgName, vaultName, tags);
            return kv;
        }

        public async Task<IVault> GetKeyVaultAsync(string rgName, string vaultName)
        {
            _logger.Information("Getting Key Vault. rgName: {rgName}, vaultName: {vaultName} ...", rgName, vaultName);
            var stor = await FluentClient
                .Vaults
                .GetByResourceGroupAsync(rgName, vaultName);

            if (stor == null)
            {
                _logger.Information("Cannot find Key Vault. rgName: {rgName}, vaultName: {vaultName} ...", rgName, vaultName);
            }

            return stor;
        }

        public async Task<IVault> GetKeyVaultByIdAsync(string kvResourceId)
        {
            try
            {
                _logger.Information($"Getting KeyVault with resource Id {kvResourceId} ...");
                return await FluentClient.Vaults.GetByIdAsync(kvResourceId);
            }
            catch (CloudException ex) when (ex.IsNotFound())
            {
                return null;
            }
        }

        public async Task<IEnumerable<IVault>> ListKeyVaultAsync(string rgName, string namePrefix = null)
        {
            _logger.Information($"Listing KeyVault in resource group {rgName} with prefix {namePrefix} ...");
            IEnumerable<IVault> kvs = (await FluentClient
                .Vaults
                .ListByResourceGroupAsync(rgName)).ToList();

            if (!string.IsNullOrEmpty(namePrefix))
            {
                kvs = kvs.Where((kv) => kv.Name.OrdinalStartsWith(namePrefix));
            }

            return kvs;
        }

        public async Task RemoveAccessPolicyAsync(string kvResourceId, string servicePrincipalObjectId)
        {
            if (string.IsNullOrEmpty(servicePrincipalObjectId))
            {
                throw new ArgumentNullException(nameof(servicePrincipalObjectId));
            }

            var vault = await GetKeyVaultByIdAsync(kvResourceId) ?? throw new InvalidOperationException("Cannt find vault with resource Id: " + kvResourceId);
            await vault.Update().WithoutAccessPolicy(servicePrincipalObjectId).ApplyAsync();
            _logger.Information("Finished removing KeyVault '{kvResourceId}' access policy of '{servicePrincipalObjectId}'", kvResourceId, servicePrincipalObjectId);
        }

        public async Task GrantSelfKeyVaultAdminAccessAsync(IVault kv)
        {
            if (string.IsNullOrEmpty(SPNObjectId))
            {
                var ex = new ArgumentException($"{nameof(SPNObjectId)} is empty. Cannot grant access.");
                _logger.Error(ex, $"Please set {nameof(SPNObjectId)} in the constructor.");
                throw ex;
            }

            await kv.Update()
                .DefineAccessPolicy()
                .ForObjectId(SPNObjectId)
                .AllowKeyAllPermissions()
                .AllowSecretAllPermissions()
                .AllowCertificateAllPermissions()
                .Attach()
                .ApplyAsync();

            _logger.Information("Granted all secret and certificate permissions (access policy) of key vault '{kvId}' to the excuting SPN with object Id '{SPNObjectId}'.", kv.Id, SPNObjectId);
        }

        public async Task RemoveSelfKeyVaultAccessAsync(IVault kv)
        {
            if (string.IsNullOrEmpty(SPNObjectId))
            {
                var ex = new ArgumentException($"{nameof(SPNObjectId)} is empty. Cannot grant access.");
                _logger.Error(ex, $"Please set {nameof(SPNObjectId)} in the constructor.");
                throw ex;
            }

            await kv.Update().WithoutAccessPolicy(SPNObjectId).ApplyAsync();

            _logger.Information("Removed access of the excuting SPN with object Id '{SPNObjectId}' to key vault '{kvId}'", SPNObjectId, kv.Id);
        }

        #endregion Key Vault

        #region Redis Cache
        public async Task<IRedisCache> GetOrCreateRedisCacheAsync(Region location, string rgName, string redisCacheName, IDictionary<string, string> tags, IDictionary<string, string> redisConfig = null)
        {
            var rc = await GetRedisCachesAsync(rgName, redisCacheName);

            if (rc == null)
            {
                rc = await CreateRedisCacheAsync(location, rgName, redisCacheName, tags, redisConfig);
            }

            return rc;
        }

        public async Task<IRedisCache> GetRedisCachesAsync(string rgName, string redisCacheName)
        {
            _logger.Information($"Getting Redis Cache. rgName: {rgName}, redisCacheName: {redisCacheName} ...");

            var redisCache = await FluentClient
                .RedisCaches
                .GetByResourceGroupAsync(rgName, redisCacheName);

            if (redisCache == null)
            {
                _logger.Information($"Cannot find Redis Cache. rgName: {rgName}, vaultName: {redisCacheName} ...");
            }

            return redisCache;
        }

        public async Task<IEnumerable<IRedisCache>> ListRedisCacheAsync(string rgName)
        {
            _logger.Information($"Listing Redis Caches in rg {rgName}...");
            return await FluentClient.RedisCaches
                .ListByResourceGroupAsync(rgName);
        }

        public async Task<IRedisCache> CreateRedisCacheAsync(Region location, string rgName, string redisCacheName, IDictionary<string, string> tags, IDictionary<string, string> redisConfig = null)
        {
            _logger.Information($"Creating a RedisCache with name {redisCacheName} ...");

            var creatable = FluentClient.RedisCaches
            .Define(redisCacheName)
            .WithRegion(location)
            .WithExistingResourceGroup(rgName)
            .WithStandardSku(1)
            .WithMinimumTlsVersion(TlsVersion.OneFullStopTwo)
            .WithTags(tags);

            if (redisConfig != null)
            {
                creatable = creatable.WithRedisConfiguration(redisConfig);
            }

            IRedisCache redisCache = await creatable.CreateAsync();
            _logger.Information($"Created RedisCache with resourceId {redisCache.Id}");

            return redisCache;
        }
        #endregion

        #region AKS
        public async Task<IKubernetesCluster> CreateAksClusterAsync(
            Region region,
            string rgName,
            string aksName,
            string rootUserName,
            string sshPublicKey,
            ContainerServiceVMSizeTypes vmSizeType,
            string k8sVersion,
            int vmCount,
            string outboundIPId,
            IDictionary<string, string> tags,
            ISubnet subnet = null,
            string agentPoolProfileName = "ap",
            bool supportAvailabilityZone = false)
        {
            Regex rx = new Regex(@"^[a-z][a-z0-9]{0,11}$");
            if (!rx.IsMatch(agentPoolProfileName))
            {
                throw new ArgumentException("Agent pool profile name does not match pattern '^[a-z][a-z0-9]{0,11}$'");
            }

            _logger.Information($"Availability Zone Support is set {supportAvailabilityZone} for the Kuberenetes Cluster");
            _logger.Information("Creating a Kubernetes cluster of version {kubernetesVersion} with name {aksName} ...", k8sVersion, aksName);
            _logger.Information($"Outbound IP {outboundIPId} is added to AKS cluster ARM Template...");

            using var ops = _logger.StartTimedOperation(nameof(CreateAksClusterAsync));
            try
            {
                var templateContent = AKSHelper.GenerateAKSTemplate(
                    region,
                    aksName,
                    k8sVersion,
                    rootUserName,
                    sshPublicKey,
                    vmSizeType.Value,
                    vmCount,
                    agentPoolProfileName,
                    tags,
                    supportAvailabilityZone,
                    outboundIPId,
                    subnet);

                await CreateDeploymentAsync(region, rgName, templateContent);

                var k8s = await GetAksClusterAsync(rgName, aksName);

                _logger.Information("Created Kubernetes cluster with resource Id {resourceId}", k8s.Id);
                return k8s;
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                _logger.Error(ex, "AKS created failed.");
                throw;
            }
        }

        public async Task<IKubernetesCluster> GetAksClusterAsync(string aksResourceId)
        {
            return await FluentClient
                .KubernetesClusters
                .GetByIdAsync(aksResourceId);
        }

        public Task<IKubernetesCluster> GetAksClusterAsync(string rgName, string aksName)
        {
            var aksId = $"subscriptions/{FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.ContainerService/managedClusters/{aksName}";
            return GetAksClusterAsync(aksId);
        }

        public async Task<IEnumerable<IKubernetesCluster>> ListAksClusterAsync(string rgName)
        {
            _logger.Information($"Listing Aks cluster in resource group {rgName} ...");
            return await FluentClient
                .KubernetesClusters
                .ListByResourceGroupAsync(rgName);
        }

        public async Task<string> GetAKSMIAsync(string rgName, string aksName)
        {
            // https://docs.microsoft.com/en-us/azure/aks/use-managed-identity
            _logger.Information($"Getting the AKS control plane managed idenity of cluster '{aksName}'");
            var aksId = $"subscriptions/{FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.ContainerService/managedClusters/{aksName}";
            var aksContent = await GetResourceAsync(aksId, "2020-04-01");
            if (string.IsNullOrEmpty(aksContent))
            {
                return null;
            }

            try
            {
                dynamic aksObject = JObject.Parse(aksContent);
                return aksObject.identity.principalId;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return null;
            }
        }

        public async Task<IEnumerable<IIdentity>> ListAKSMCMIAsync(string AKSRGName, string AKSName, Region location)
        {
            // https://docs.microsoft.com/en-us/azure/aks/use-managed-identity
            _logger.Information($"Listing the AKS managed identities in 'MC_' resource group '{AKSRGName}'");
            var mcRG = NamingContext.AKSMCResourceGroupName(AKSRGName, AKSName, location);
            return await FluentClient.Identities.ListByResourceGroupAsync(mcRG);
        }
        #endregion AKS

        #region Identity
        public async Task<IIdentity> GetOrCreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags)
        {
            var msi = await GetMSIAsync(rgName, msiName);
            if (msi == null)
            {
                msi = await CreateMSIAsync(location, rgName, msiName, tags);
            }

            return msi;
        }

        public async Task<IIdentity> CreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags)
        {
            _logger.Information("Creating a Managed Identity with name {msiName} ...", msiName);
            using var ops = _logger.StartTimedOperation(nameof(CreateMSIAsync));
            try
            {
                var msi = await FluentClient.Identities
                    .Define(msiName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(rgName)
                    .WithTags(tags)
                    .CreateAsync();

                _logger.Information("Created Managed Identity with Id {ResourceId} ...", msi.Id);
                return msi;
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public Task<IIdentity> GetMSIAsync(string rgName, string msiName)
        {
            _logger.Information("Getting Managed Identity with name {msiName} in RG {rgName} ...", msiName, rgName);
            return FluentClient.Identities.GetByResourceGroupAsync(rgName, msiName);
        }
        #endregion

        #region ACR
        public async Task<IRegistry> GetOrCreateACRAsync(Region location, string rgName, string acrName, IDictionary<string, string> tags)
        {
            var acr = await GetACRAsync(rgName, acrName);

            if (acr == null)
            {
                var helper = new ACRHelper(_logger);
                acr = await helper.CreateACRAsync(this, location, rgName, acrName, tags);
                _logger.Information("Created ACR with Id {resourceId}.", acr.Id);
            }

            return acr;
        }

        public Task<IRegistry> GetACRAsync(string rgName, string acrName)
        {
            _logger.Information("Getting the ACR {acrName} in RG {rgName} ...", acrName, rgName);
            return FluentClient.ContainerRegistries.GetByResourceGroupAsync(rgName, acrName);
        }
        #endregion

        #region Deployments
        public async Task<IDeployment> CreateDeploymentAsync(Region location, string rgName, string template, string templateParameters = null, bool noLogging = false, CancellationToken cancellationToken = default)
        {
            var deploymentName = SdkContext.RandomResourceName("LiftrFluentSDK", 24);
            if (string.IsNullOrEmpty(template))
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (string.IsNullOrEmpty(templateParameters))
            {
                templateParameters = "{}";
            }

            _logger.Information($"Starting an incremental ARM deployment with name {deploymentName} ...");
            if (!noLogging)
            {
                _logger.Information("Deployment template: {@template}", template);
                _logger.Information("Deployment template Parameters: {@templateParameters}", templateParameters);
            }

            try
            {
                var deployment = await FluentClient.Deployments
                    .Define(deploymentName)
                    .WithExistingResourceGroup(rgName)
                    .WithTemplate(template)
                    .WithParameters(templateParameters)
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync(cancellationToken);

                _logger.Information($"Finished the ARM deployment with name {deploymentName} ...");
                return deployment;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ARM deployment failed");
                if (ex is CloudException)
                {
                    var cloudEx = ex as CloudException;
                    _logger.Error("Failure details: " + cloudEx.Response.Content);
                }

                var error = await DeploymentExtensions.GetDeploymentErrorDetailsAsync(FluentClient.SubscriptionId, rgName, deploymentName, AzureCredentials);
                _logger.Error("ARM deployment with name {@deploymentName} Failed with Error: {@DeploymentError}", deploymentName, error);
                throw new ARMDeploymentFailureException("ARM deployment failed", ex) { Details = error };
            }
        }
        #endregion

        #region Monitoring
        public async Task<string> GetOrCreateLogAnalyticsWorkspaceAsync(Region location, string rgName, string name, IDictionary<string, string> tags)
        {
            var logAnalytics = await GetLogAnalyticsWorkspaceAsync(rgName, name);
            if (logAnalytics == null)
            {
                var helper = new LogAnalyticsHelper(_logger);
                await helper.CreateLogAnalyticsWorkspaceAsync(this, location, rgName, name, tags);
                logAnalytics = await GetLogAnalyticsWorkspaceAsync(rgName, name);
                _logger.Information("Created a new Log Analytics Workspace");
            }

            return logAnalytics;
        }

        public Task<string> GetLogAnalyticsWorkspaceAsync(string rgName, string name)
        {
            var helper = new LogAnalyticsHelper(_logger);
            return helper.GetLogAnalyticsWorkspaceAsync(this, rgName, name);
        }

        public async Task<IActionGroup> GetOrUpdateActionGroupAsync(string rgName, string name, string receiverName, string email)
        {
            _logger.Information("Getting Action Group. rgName: {rgName}, name: {name} ...", rgName, name);
            IActionGroup ag;
            try
            {
                var subId = FluentClient.GetCurrentSubscription();
                ag = await FluentClient
                    .ActionGroups.GetByIdAsync($"/subscriptions/{subId}/resourceGroups/{rgName}/providers/microsoft.insights/actionGroups/{name}");
            }
            catch (Azure.Management.Monitor.Fluent.Models.ErrorResponseException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Information("Creating a Action Group. rgName: {rgName}, name: {name} ...", rgName, name);
                ag = await FluentClient.ActionGroups.Define(name)
                    .WithExistingResourceGroup(rgName)
                    .DefineReceiver(receiverName)
                    .WithEmail(email)
                    .Attach()
                    .CreateAsync();
            }

            return ag;
        }

        public async Task<IMetricAlert> GetOrUpdateMetricAlertAsync(string rgName, MetricAlertOptions alertOptions)
        {
            _logger.Information("Getting Metric Alert. rgName: {rgName}, name: {name} ...", rgName, alertOptions.Name);
            IMetricAlert ma;
            try
            {
                var subId = FluentClient.GetCurrentSubscription();
                ma = await FluentClient
                    .AlertRules.MetricAlerts.GetByIdAsync($"/subscriptions/{subId}/resourceGroups/{rgName}/providers/microsoft.insights/scheduledqueryrules/{alertOptions.Name}");
            }
            catch (Azure.Management.Monitor.Fluent.Models.ErrorResponseException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Information("Creating a Metric Alert. rgName: {rgName}, name: {name} ...", rgName, alertOptions.Name);
                ma = await FluentClient.AlertRules.MetricAlerts.Define(alertOptions.Name)
                    .WithExistingResourceGroup(rgName)
                    .WithTargetResource(alertOptions.TargetResourceId)
                    .WithPeriod(TimeSpan.FromMinutes(alertOptions.AggregationPeriod))
                    .WithFrequency(TimeSpan.FromMinutes(alertOptions.FrequencyOfEvaluation))
                    .WithAlertDetails(alertOptions.Severity, alertOptions.Description)
                    .WithActionGroups(alertOptions.ActionGroupResourceId)
                    .DefineAlertCriteria(alertOptions.AlertConditionName)
                    .WithMetricName(alertOptions.MetricName, alertOptions.MetricNamespace)
                    .WithCondition(MetricAlertRuleTimeAggregation.Parse(alertOptions.TimeAggregationType), MetricAlertRuleCondition.Parse(alertOptions.ConditionOperator), alertOptions.Threshold)
                    .Attach()
                    .CreateAsync();
            }

            return ma;
        }
        #endregion

        #region Event Hub
        public async Task<IEventHubNamespace> GetOrCreateEventHubNamespaceAsync(Region location, string rgName, string name, int throughtputUnits, int maxThroughtputUnits, IDictionary<string, string> tags)
        {
            _logger.Information("Getting Event hub namespace. rgName: {rgName}, name: {name} ...", rgName, name);
            IEventHubNamespace eventHubNamespace = null;
            try
            {
                eventHubNamespace = await FluentClient
                    .EventHubNamespaces
                    .GetByResourceGroupAsync(rgName, name);
            }
            catch (Azure.Management.EventHub.Fluent.Models.ErrorResponseException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Information("Creating a Event hub namespace. rgName: {rgName}, name: {name} ...", rgName, name);
                eventHubNamespace = await FluentClient
                    .EventHubNamespaces
                    .Define(name)
                    .WithRegion(location)
                    .WithExistingResourceGroup(rgName)
                    .WithAutoScaling()
                    .WithCurrentThroughputUnits(throughtputUnits)
                    .WithThroughputUnitsUpperLimit(maxThroughtputUnits)
                    .WithTags(tags)
                    .CreateAsync();
            }

            return eventHubNamespace;
        }

        public async Task<IEventHub> GetOrCreateEventHubAsync(Region location, string rgName, string namespaceName, string hubName, int partitionCount, int throughtputUnits, int maxThroughtputUnits, IList<string> consumerGroups, IDictionary<string, string> tags)
        {
            _logger.Information("Getting Event Hub. rgName: {rgName}, namespaceName: {namespaceName}, hubName: {hubName} ...", rgName, namespaceName, hubName);
            IEventHub eventhub = null;

            if (consumerGroups == null)
            {
                throw new ArgumentNullException(nameof(consumerGroups));
            }

            try
            {
                eventhub = await FluentClient
                    .EventHubs
                    .GetByNameAsync(rgName, namespaceName, hubName);
            }
            catch (Azure.Management.EventHub.Fluent.Models.ErrorResponseException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Information("Cannot find Event Hub. rgName: {rgName}, namespaceName: {namespaceName}, hubName: {hubName} ...", rgName, namespaceName, hubName);
                IEventHubNamespace eventHubNamespace = await GetOrCreateEventHubNamespaceAsync(location, rgName, namespaceName, throughtputUnits, maxThroughtputUnits, tags);

                _logger.Information($"Creating a Event Hub with namespaceName {namespaceName}, name {hubName} ...", namespaceName, hubName);

                var eventHubBuilder = FluentClient
                    .EventHubs
                    .Define(hubName)
                    .WithExistingNamespace(eventHubNamespace)
                    .WithPartitionCount(partitionCount);

                foreach (var consumerGroup in consumerGroups)
                {
                    eventHubBuilder.WithNewConsumerGroup(consumerGroup);
                }

                eventhub = await eventHubBuilder.CreateAsync();
            }

            return eventhub;
        }
        #endregion

        #region Shared Image Gallery
        public async Task<IGalleryImageVersion> GetImageVersionAsync(
            string rgName,
            string galleryName,
            string imageName,
            string imageVersionName)
        {
            if (imageName == null)
            {
                throw new ArgumentNullException(nameof(imageName));
            }

            _logger.Information("Getting image verion. imageVersionName:{imageVersionName}, galleryName: {galleryName}, imageName: {imageName}", imageVersionName, galleryName, imageName);

            try
            {
                var galleryImageVersion = await FluentClient.GalleryImageVersions
                .GetByGalleryImageAsync(rgName, galleryName, imageName, imageVersionName);
                return galleryImageVersion;
            }
            catch (CloudException ex) when (ex.IsNotFound())
            {
                return null;
            }
        }
        #endregion

        public async Task<string> WaitAsyncOperationAsync(
           HttpClient httpClient,
           HttpResponseMessage startOperationResponse,
           CancellationToken cancellationToken,
           TimeSpan? pollingTime = null)
        {
            string statusUrl = string.Empty;

            if (startOperationResponse.Headers.Contains("Location"))
            {
                statusUrl = startOperationResponse.Headers.GetValues("Location").FirstOrDefault();
            }

            if (string.IsNullOrEmpty(statusUrl) && startOperationResponse.Headers.Contains("Azure-AsyncOperation"))
            {
                statusUrl = startOperationResponse.Headers.GetValues("Azure-AsyncOperation").FirstOrDefault();
            }

            if (string.IsNullOrEmpty(statusUrl))
            {
                var ex = new InvalidOperationException("Cannot find the async status url from both the headers: Location, AsyncOperation");
                _logger.LogError(ex.Message);
                throw ex;
            }

            while (true)
            {
                var statusResponse = await _options.HttpPolicy.ExecuteAsync((ct) => httpClient.GetAsync(new Uri(statusUrl), ct), cancellationToken);
                var body = await statusResponse.Content.ReadAsStringAsync();
                bool keepWaiting = false;

                if (body.OrdinalContains("Running") ||
                    body.OrdinalContains("InProgress"))
                {
                    keepWaiting = true;
                }
                else if (body.OrdinalContains("Succeeded") ||
                    body.OrdinalContains("Failed") ||
                    body.OrdinalContains("Canceled") ||
                    (statusResponse.StatusCode != HttpStatusCode.Accepted && statusResponse.StatusCode != HttpStatusCode.Created))
                {
                    keepWaiting = false;
                }
                else
                {
                    keepWaiting = true;
                }

                if (keepWaiting)
                {
                    var retryAfter = pollingTime.HasValue ? pollingTime.Value : GetRetryAfterValue(statusResponse);
                    _logger.Information($"Wait for {retryAfter.TotalSeconds} seconds before checking the async status at: '{statusUrl}'");
                    await Task.Delay(retryAfter, cancellationToken);
                }
                else
                {
                    return body;
                }
            }
        }

        public bool IsAMETenant()
        {
            return TenantId.OrdinalEquals("33e01921-4d64-4f8c-a055-5bdaffd5e33d");
        }

        public bool IsMicrosoftTenant()
        {
            return TenantId.OrdinalEquals("72f988bf-86f1-41af-91ab-2d7cd011db47");
        }

        private TimeSpan GetRetryAfterValue(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter == null)
            {
                return TimeSpan.FromSeconds(10);
            }

            return retryAfter.Value;
        }

        private string GetStorageBlobDataContributorRoleDefinitionId()
            => $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe"; // Storage Blob Data Contributor

        private string GetStorageBlobDataReaderRoleDefinitionId()
            => $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/2a2b9908-6ea1-4ae2-8e65-a410df84e7d1"; // Storage Blob Data Reader

        private string GetStorageQueueDataContributorRoleDefinitionId()
            => $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/974c5e8b-45b9-4653-ba55-5f855dd0fb88"; // Storage Queue Data Contributor

        private string GetStorageAccountKeyOperatorRoleDefinitionId()
            => $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/81a9662b-bebf-436f-a333-f67b29880f12"; // Storage Account Key Operator Service Role
    }
}
