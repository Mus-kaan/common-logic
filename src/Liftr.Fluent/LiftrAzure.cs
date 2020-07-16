//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Eventhub.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Rest.Azure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.Management.Fluent.Azure;

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
            string spnObjectId,
            TokenCredential tokenCredential,
            AzureCredentials credentials,
            IAzure fluentClient,
            IAuthenticated authenticated,
            LiftrAzureOptions options,
            ILogger logger)
        {
            TenantId = tenantId;
            SPNObjectId = spnObjectId;
            TokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
            AzureCredentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            FluentClient = fluentClient ?? throw new ArgumentNullException(nameof(fluentClient));
            Authenticated = authenticated ?? throw new ArgumentNullException(nameof(authenticated));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string TenantId { get; }

        public string SPNObjectId { get; }

        public string DefaultSubnetName { get; } = "default";

        public IAzure FluentClient { get; }

        public IAuthenticated Authenticated { get; }

        public TokenCredential TokenCredential { get; }

        public AzureCredentials AzureCredentials { get; }

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
                var runOutputResponse = await httpClient.GetAsync(uriBuilder.Uri);

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

            // https://github.com/Azure/azure-rest-api-specs-pr/blob/87dbc20106afce8c615113d654c14359a3356486/specification/imagebuilder/resource-manager/Microsoft.VirtualMachineImages/preview/2019-05-01-preview/imagebuilder.json#L280
            using (var handler = new AzureApiAuthHandler(AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                var uriBuilder = new UriBuilder(AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path = resourceId;
                uriBuilder.Query = $"api-version={apiVersion}";
                _logger.Information($"Start deleting resource at Uri: {uriBuilder.Uri}");
                var deleteResponse = await httpClient.DeleteAsync(uriBuilder.Uri, cancellationToken);
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
                .WithTags(tags);

            if (!string.IsNullOrEmpty(accessFromSubnetId))
            {
                storageAccountCreatable = storageAccountCreatable
                    .WithAccessFromSelectedNetworks()
                    .WithAccessFromNetworkSubnet(accessFromSubnetId);
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
                // Storage Blob Data Contributor
                var roleDefinitionId = $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe";
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(roleDefinitionId)
                              .WithResourceGroupScope(rg)
                              .CreateAsync();
                _logger.Information("Granted 'Storage Blob Data Contributor' of Resource Group '{rgId}' to SPN with object Id {objectId}. roleDefinitionId: {roleDefinitionId}", rg.Id, objectId, roleDefinitionId);
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

        public async Task GrantBlobContainerContributorAsync(IStorageAccount storageAccount, string containerName, string objectId)
        {
            try
            {
                // Storage Blob Data Contributor
                var roleDefinitionId = $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe";
                var containerId = $"{storageAccount.Id}/blobServices/default/containers/{containerName}";
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(roleDefinitionId)
                              .WithScope(containerId)
                              .CreateAsync();
                _logger.Information("Granted 'Storage Blob Data Contributor' of blob container '{containerName}' to SPN with object Id {objectId}. roleDefinitionId: {roleDefinitionId}, containerId: {containerId}", containerName, objectId, roleDefinitionId, containerId);
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
                // Storage Blob Data Reader
                var roleDefinitionId = $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/2a2b9908-6ea1-4ae2-8e65-a410df84e7d1";
                var containerId = $"{storageAccount.Id}/blobServices/default/containers/{containerName}";
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(roleDefinitionId)
                              .WithScope(containerId)
                              .CreateAsync();
                _logger.Information("Granted 'Storage Blob Data Reader' of blob container '{containerName}' to SPN with object Id '{objectId}'. roleDefinitionId: {roleDefinitionId}, containerId: {containerId}", containerName, objectId, roleDefinitionId, containerId);
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

        public Task GrantQueueContributorAsync(IStorageAccount storageAccount, IIdentity msi)
            => GrantQueueContributorAsync(storageAccount, msi.GetObjectId());

        public async Task GrantQueueContributorAsync(IStorageAccount storageAccount, string objectId)
        {
            try
            {
                // Storage Queue Data Contributor
                var roleDefinitionId = $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/974c5e8b-45b9-4653-ba55-5f855dd0fb88";
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithRoleDefinition(roleDefinitionId)
                              .WithScope(storageAccount.Id)
                              .CreateAsync();
                _logger.Information("Granted 'Storage Queue Data Contributor' storage account '{resourceId}' to SPN with object Id {objectId}. roleDefinitionId: {roleDefinitionId}", storageAccount.Id, objectId, roleDefinitionId);
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
                // Storage Account Key Operator Service Role
                var roleDefinitionId = $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/81a9662b-bebf-436f-a333-f67b29880f12";
                _logger.Information("Assigning 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId} ...", roleDefinitionId, _options.AzureKeyVaultObjectId);
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(_options.AzureKeyVaultObjectId)
                              .WithRoleDefinition(roleDefinitionId)
                              .WithScope(storageAccount.Id)
                              .CreateAsync();
                _logger.Information("Granted 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId}", roleDefinitionId, _options.AzureKeyVaultObjectId);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
        }

        public async Task DelegateStorageKeyOperationToKeyVaultAsync(IResourceGroup rg)
        {
            try
            {
                // Storage Account Key Operator Service Role
                var roleDefinitionId = $"/subscriptions/{FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/81a9662b-bebf-436f-a333-f67b29880f12";
                _logger.Information("Assigning 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId} on rg: {rgId} ...", roleDefinitionId, _options.AzureKeyVaultObjectId, rg.Id);
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(_options.AzureKeyVaultObjectId)
                              .WithRoleDefinition(roleDefinitionId)
                              .WithResourceGroupScope(rg)
                              .CreateAsync();
                _logger.Information("Granted 'Storage Account Key Operator Service Role' {roleDefinitionId} to Key Vault's First party App with objectId: {objectId} on rg: {rgId}", roleDefinitionId, _options.AzureKeyVaultObjectId, rg.Id);
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
                .AllowAny80InBound()
                .AllowAny443InBound()
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

        public async Task<IPublicIPAddress> GetOrCreatePublicIPAsync(Region location, string rgName, string pipName, IDictionary<string, string> tags)
        {
            var pip = await GetPublicIPAsync(rgName, pipName);
            if (pip == null)
            {
                pip = await CreatePublicIPAsync(location, rgName, pipName, tags);
            }

            return pip;
        }

        public async Task<IPublicIPAddress> CreatePublicIPAsync(Region location, string rgName, string pipName, IDictionary<string, string> tags)
        {
            _logger.Information("Start creating Publib IP address with name: {pipName} in RG: {rgName} ...", pipName, rgName);

            var pip = await FluentClient
                .PublicIPAddresses
                .Define(pipName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithStaticIP()
                .WithLeafDomainLabel(pipName)
                .WithTags(tags)
                .CreateAsync();

            _logger.Information("Created Publib IP address with resourceId: {resourceId}", pip.Id);
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

            _logger.Information("Created DNS zone with id '{resourceId}'.", dns);
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
            _logger.Information($"Creating a CosmosDB with name {cosmosDBName} ...");
            var creatable = FluentClient
               .CosmosDBAccounts
               .Define(cosmosDBName)
               .WithRegion(location)
               .WithExistingResourceGroup(rgName)
               .WithDataModelMongoDB()
               .WithStrongConsistency()
               .WithTags(tags);

            if (subnet != null)
            {
                creatable = creatable.WithVirtualNetworkRule(subnet.Parent.Id, subnet.Name);
            }

            ICosmosDBAccount cosmosDBAccount = await creatable.CreateAsync();
            _logger.Information($"Created CosmosDB with name {cosmosDBName}");

            _logger.Information("Get the MongoDB connection string");
            var databaseAccountListConnectionStringsResult = await cosmosDBAccount.ListConnectionStringsAsync();
            var mongoConnectionString = databaseAccountListConnectionStringsResult.ConnectionStrings[0].ConnectionString;

            return (cosmosDBAccount, mongoConnectionString);
        }

        public Task<ICosmosDBAccount> GetCosmosDBAsync(string dbResourceId)
        {
            return FluentClient
                .CosmosDBAccounts
                .GetByIdAsync(dbResourceId);
        }

        public Task<ICosmosDBAccount> GetCosmosDBAsync(string rgName, string cosmosDBName)
        {
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

        public async Task WithKeyVaultAccessFromNetworkAsync(IVault vault, string ipAddress, string subnetId)
        {
            var helper = new KeyVaultHelper(_logger);
            await helper.WithAccessFromNetworkAsync(vault, this, ipAddress, subnetId);
        }

        public async Task<IVault> CreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags)
        {
            _logger.Information("Creating a Key Vault with name {vaultName} ...", vaultName);

            IVault vault = await FluentClient.Vaults
                        .Define(vaultName)
                        .WithRegion(location)
                        .WithExistingResourceGroup(rgName)
                        .WithEmptyAccessPolicy()
                        .WithTags(tags)
                        .WithDeploymentDisabled()
                        .WithTemplateDeploymentDisabled()
                        .CreateAsync();

            _logger.Information("Created Key Vault with resourceId {resourceId}", vault.Id);

            return vault;
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

        #region AKS
        public async Task<IKubernetesCluster> CreateAksClusterAsync(
            Region region,
            string rgName,
            string aksName,
            string rootUserName,
            string sshPublicKey,
            string servicePrincipalClientId,
            string servicePrincipalSecret,
            ContainerServiceVMSizeTypes vmSizeType,
            int vmCount,
            IDictionary<string, string> tags,
            ISubnet subnet = null,
            string agentPoolProfileName = "ap")
        {
            Regex rx = new Regex(@"^[a-z][a-z0-9]{0,11}$");
            if (!rx.IsMatch(agentPoolProfileName))
            {
                throw new ArgumentException("Agent pool profile name does not match pattern '^[a-z][a-z0-9]{0,11}$'");
            }

            _logger.Information("Creating a Kubernetes cluster of version {kubernetesVersion} with name {aksName} ...", _options.KubernetesVersion, aksName);

            var creatable = FluentClient.KubernetesClusters
                             .Define(aksName)
                             .WithRegion(region)
                             .WithExistingResourceGroup(rgName)
                             .WithVersion(_options.KubernetesVersion)
                             .WithRootUsername(rootUserName)
                             .WithSshKey(sshPublicKey)
                             .WithServicePrincipalClientId(servicePrincipalClientId)
                             .WithServicePrincipalSecret(servicePrincipalSecret)
                             .DefineAgentPool(agentPoolProfileName)
                             .WithVirtualMachineSize(vmSizeType)
                             .WithAgentPoolVirtualMachineCount(vmCount);

            IKubernetesCluster k8s = null;
            if (subnet == null)
            {
                k8s = await creatable
                    .Attach()
                    .WithDnsPrefix(aksName)
                    .WithTags(tags)
                    .CreateAsync();
            }
            else
            {
                _logger.Information("Restrict the AKS agent pool in subnet '{subnetName}' of VNet '{vnetId}'", subnet.Name, subnet.Parent.Id);
                k8s = await creatable
                    .WithVirtualNetwork(subnet.Parent.Id, subnet.Name)
                    .Attach()
                    .WithDnsPrefix(aksName)
                    .WithTags(tags)
                    .CreateAsync();
            }

            _logger.Information("Created Kubernetes cluster with resource Id {resourceId}", k8s.Id);
            return k8s;
        }

        public async Task<IKubernetesCluster> GetAksClusterAsync(string aksResourceId)
        {
            return await FluentClient
                .KubernetesClusters
                .GetByIdAsync(aksResourceId);
        }

        public async Task<IEnumerable<IKubernetesCluster>> ListAksClusterAsync(string rgName)
        {
            _logger.Information($"Listing Aks cluster in resource group {rgName} ...");
            return await FluentClient
                .KubernetesClusters
                .ListByResourceGroupAsync(rgName);
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
            var msi = await FluentClient.Identities
                .Define(msiName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithTags(tags)
                .CreateAsync();
            _logger.Information("Created Managed Identity with Id {ResourceId} ...", msi.Id);
            return msi;
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
                _logger.Information("Creating ACR with name {acrName} in RG {rgName} ...", acrName, rgName);
                acr = await FluentClient.ContainerRegistries
                    .Define(acrName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(rgName)
                    .WithPremiumSku()
                    .WithTags(tags)
                    .CreateAsync();
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
        public async Task<IDeployment> CreateDeploymentAsync(Region location, string rgName, string template, string templateParameters = null, bool noLogging = false)
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
                    .CreateAsync();

                _logger.Information($"Finished the ARM deployment with name {deploymentName} ...");
                return deployment;
            }
            catch (Exception ex)
            {
                _logger.Error("ARM deployment failed", ex);
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
        #endregion

        #region Event Hub
        public async Task<IEventHubNamespace> GetOrCreateEventHubNamespaceAsync(Region location, string rgName, string name, IDictionary<string, string> tags)
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
                    .WithTags(tags)
                    .CreateAsync();
            }

            return eventHubNamespace;
        }

        public async Task<IEventHub> GetOrCreateEventHubAsync(Region location, string rgName, string namespaceName, string hubName, int partitionCount, IList<string> consumerGroups, IDictionary<string, string> tags)
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
                IEventHubNamespace eventHubNamespace = await GetOrCreateEventHubNamespaceAsync(location, rgName, namespaceName, tags);

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

        public async Task<string> WaitAsyncOperationAsync(
           HttpClient client,
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
                var statusResponse = await client.GetAsync(new Uri(statusUrl), cancellationToken);
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

        private TimeSpan GetRetryAfterValue(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter == null)
            {
                return TimeSpan.FromSeconds(10);
            }

            return retryAfter.Value;
        }
    }
}
