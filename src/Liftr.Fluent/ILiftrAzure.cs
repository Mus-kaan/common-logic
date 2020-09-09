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
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.Redis.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.Management.Fluent.Azure;

namespace Microsoft.Liftr.Fluent
{
    public interface ILiftrAzure
    {
        IAzure FluentClient { get; }

        IAuthenticated Authenticated { get; }

        TokenCredential TokenCredential { get; }

        AzureCredentials AzureCredentials { get; }

        LiftrAzureOptions Options { get; }

        string TenantId { get; }

        string SPNObjectId { get; }

        string DefaultSubnetName { get; }

        Task<string> GetResourceAsync(string resourceId, string apiVersion);

        Task DeleteResourceAsync(string resourceId, string apiVersion, CancellationToken cancellationToken = default);

        Task<string> WaitAsyncOperationAsync(HttpClient client, HttpResponseMessage startOperationResponse, CancellationToken cancellationToken, TimeSpan? pollingTime = null);

        #region Resource Group
        Task<IResourceGroup> GetOrCreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags);

        Task<IResourceGroup> CreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags);

        Task<IResourceGroup> GetResourceGroupAsync(string rgName);

        Task DeleteResourceGroupAsync(string rgName, bool noThrow = false);

        Task DeleteResourceGroupWithTagAsync(string tagName, string tagValue, Func<IReadOnlyDictionary<string, string>, bool> tagsFilter = null);
        #endregion Resource Group

        #region Storage Account
        Task<IStorageAccount> GetOrCreateStorageAccountAsync(Region location, string rgName, string storageAccountName, IDictionary<string, string> tags, string accessFromSubnetId = null);

        Task<IStorageAccount> CreateStorageAccountAsync(Region location, string rgName, string storageAccountName, IDictionary<string, string> tags, string accessFromSubnetId = null);

        Task<IStorageAccount> GetStorageAccountAsync(string rgName, string storageAccountName);

        Task<IEnumerable<IStorageAccount>> ListStorageAccountAsync(string rgName, string namePrefix = null);

        Task GrantBlobContributorAsync(IResourceGroup rg, string objectId);

        Task GrantBlobContributorAsync(IResourceGroup rg, IIdentity msi);

        Task GrantBlobContainerContributorAsync(IStorageAccount storageAccount, string containerName, string objectId);

        Task GrantBlobContainerContributorAsync(IStorageAccount storageAccount, string containerName, IIdentity msi);

        Task GrantBlobContainerReaderAsync(IStorageAccount storageAccount, string containerName, string objectId);

        Task GrantBlobContainerReaderAsync(IStorageAccount storageAccount, string containerName, IIdentity msi);

        Task GrantQueueContributorAsync(IStorageAccount storageAccount, IIdentity msi);

        Task GrantQueueContributorAsync(IStorageAccount storageAccount, string objectId);

        Task DelegateStorageKeyOperationToKeyVaultAsync(IStorageAccount storageAccount);

        Task DelegateStorageKeyOperationToKeyVaultAsync(IResourceGroup rg);
        #endregion Storage Account

        #region Network
        Task<INetworkSecurityGroup> GetNSGAsync(string rgName, string nsgName);

        Task<INetworkSecurityGroup> GetOrCreateDefaultNSGAsync(Region location, string rgName, string nsgName, IDictionary<string, string> tags);

        Task<INetwork> GetOrCreateVNetAsync(Region location, string rgName, string vnetName, IDictionary<string, string> tags, string nsgId = null);

        Task<INetwork> CreateVNetAsync(Region location, string rgName, string vnetName, IDictionary<string, string> tags, string nsgId = null);

        Task<INetwork> GetVNetAsync(string rgName, string vnetName);

        Task<ISubnet> CreateNewSubnetAsync(INetwork vnet, string subnetName, string nsgId = null);

        Task<IPublicIPAddress> GetOrCreatePublicIPAsync(Region location, string rgName, string pipName, IDictionary<string, string> tags, PublicIPSkuType skuType = null);

        Task<IPublicIPAddress> CreatePublicIPAsync(Region location, string rgName, string pipName, IDictionary<string, string> tags, PublicIPSkuType skuType = null);

        Task<IPublicIPAddress> GetPublicIPAsync(string rgName, string pipName);

        Task<IEnumerable<IPublicIPAddress>> ListPublicIPAsync(string rgName, string namePrefix = null);

        Task<ITrafficManagerProfile> GetOrCreateTrafficManagerAsync(string rgName, string tmName, IDictionary<string, string> tags);

        Task<ITrafficManagerProfile> CreateTrafficManagerAsync(string rgName, string tmName, IDictionary<string, string> tags);

        Task<ITrafficManagerProfile> GetTrafficManagerAsync(string tmId);

        Task<ITrafficManagerProfile> GetTrafficManagerAsync(string rgName, string tmName);

        Task<IDnsZone> GetDNSZoneAsync(string rgName, string dnsName);

        Task<IDnsZone> CreateDNSZoneAsync(string rgName, string dnsName, IDictionary<string, string> tags);
        #endregion

        #region CosmosDB
        Task<(ICosmosDBAccount cosmosDBAccount, string mongoConnectionString)> CreateCosmosDBAsync(
            Region location,
            string rgName,
            string cosmosDBName,
            IDictionary<string, string> tags,
            ISubnet subnet = null);

        Task<ICosmosDBAccount> GetCosmosDBAsync(string dbResourceId);

        Task<ICosmosDBAccount> GetCosmosDBAsync(string rgName, string cosmosDBName);

        Task<IEnumerable<ICosmosDBAccount>> ListCosmosDBAsync(string rgName);
        #endregion CosmosDB

        #region Key Vault
        Task<IVault> GetOrCreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags);

        Task<IVault> GetOrCreateKeyVaultAsync(Region location, string rgName, string vaultName, string accessibleFromIP, IDictionary<string, string> tags);

        Task<IVault> CreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags);

        Task<IVault> GetKeyVaultAsync(string rgName, string vaultName);

        Task<IVault> GetKeyVaultByIdAsync(string kvResourceId);

        Task<IEnumerable<IVault>> ListKeyVaultAsync(string rgName, string namePrefix = null);

        Task WithKeyVaultAccessFromNetworkAsync(IVault vault, string ipAddress, string subnetId);

        Task RemoveAccessPolicyAsync(string kvResourceId, string servicePrincipalObjectId);

        Task GrantSelfKeyVaultAdminAccessAsync(IVault kv);

        Task RemoveSelfKeyVaultAccessAsync(IVault kv);
        #endregion Key Vault

        #region Redis Cache
        Task<IRedisCache> GetOrCreateRedisCacheAsync(Region location, string rgName, string redisCacheName, IDictionary<string, string> tags, IDictionary<string, string> redisConfig = null);

        Task<IRedisCache> GetRedisCachesAsync(string rgName, string redisCacheName);

        Task<IEnumerable<IRedisCache>> ListRedisCacheAsync(string rgName);

        Task<IRedisCache> CreateRedisCacheAsync(Region location, string rgName, string redisCacheName, IDictionary<string, string> tags, IDictionary<string, string> redisConfig = null);
        #endregion

        #region AKS
        Task<IKubernetesCluster> CreateAksClusterAsync(
            Region region,
            string rgName,
            string aksName,
            string rootUserName,
            string sshPublicKey,
            ContainerServiceVMSizeTypes vmSizeType,
            int vmCount,
            IDictionary<string, string> tags,
            ISubnet subnet = null,
            string agentPoolProfileName = "ap");

        Task<IKubernetesCluster> GetAksClusterAsync(string aksResourceId);

        Task<IKubernetesCluster> GetAksClusterAsync(string rgName, string aksName);

        Task<IEnumerable<IKubernetesCluster>> ListAksClusterAsync(string rgName);

        Task<string> GetAKSMIAsync(string rgName, string aksName);

        Task<IEnumerable<IIdentity>> ListAKSMCMIAsync(string AKSRGName, string AKSName, Region location);
        #endregion AKS

        #region Identity
        Task<IIdentity> GetOrCreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags);

        Task<IIdentity> CreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags);

        Task<IIdentity> GetMSIAsync(string rgName, string msiName);
        #endregion

        #region ACR
        Task<IRegistry> GetOrCreateACRAsync(Region location, string rgName, string acrName, IDictionary<string, string> tags);

        Task<IRegistry> GetACRAsync(string rgName, string acrName);
        #endregion

        #region Deployments
        Task<IDeployment> CreateDeploymentAsync(Region location, string rgName, string template, string templateParameters = null, bool noLogging = false, CancellationToken cancellationToken = default);
        #endregion

        #region Monitoring
        Task<string> GetOrCreateLogAnalyticsWorkspaceAsync(Region location, string rgName, string name, IDictionary<string, string> tags);

        Task<string> GetLogAnalyticsWorkspaceAsync(string rgName, string name);
        #endregion

        #region Event Hub
        Task<IEventHubNamespace> GetOrCreateEventHubNamespaceAsync(Region location, string rgName, string name, int throughtputUnits, int maxThroughtputUnits, IDictionary<string, string> tags);

        Task<IEventHub> GetOrCreateEventHubAsync(Region location, string rgName, string namespaceName, string hubName, int partitionCount, int throughtputUnits, int maxThroughtputUnits, IList<string> consumerGroups, IDictionary<string, string> tags);
        #endregion

        #region Shared Image Gallery
        Task<IGalleryImageVersion> GetImageVersionAsync(
            string rgName,
            string galleryName,
            string imageName,
            string imageVersionName);
        #endregion
    }
}
