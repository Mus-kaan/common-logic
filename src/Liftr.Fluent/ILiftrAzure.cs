//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Eventhub.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.Redis.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Contracts.AzureMonitor;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.Management.Fluent.Azure;
using TimeSpan = System.TimeSpan;

namespace Microsoft.Liftr.Fluent
{
    public interface ILiftrAzure
    {
        Serilog.ILogger Logger { get; }

        IAzure FluentClient { get; }

        IAuthenticated Authenticated { get; }

        TokenCredential TokenCredential { get; }

        AzureCredentials AzureCredentials { get; }

        LiftrAzureOptions Options { get; }

        string TenantId { get; }

        string DefaultSubscriptionId { get; }

        string SPNObjectId { get; }

        string DefaultSubnetName { get; }

        bool IsAMETenant();

        bool IsMicrosoftTenant();

        Task<string> GetResourceAsync(string resourceId, string apiVersion, CancellationToken cancellationToken = default);

        Task PutResourceAsync(string resourceId, string apiVersion, string resourceBody, CancellationToken cancellationToken = default);

        Task PatchResourceAsync(string resourceId, string apiVersion, string resourceJsonBody, CancellationToken cancellationToken = default);

        Task DeleteResourceAsync(string resourceId, string apiVersion, CancellationToken cancellationToken = default);

        Task<string> WaitAsyncOperationAsync(HttpClient client, HttpResponseMessage startOperationResponse, CancellationToken cancellationToken, TimeSpan? pollingTime = null);

        #region Resource provider
        Task<string> GetResourceProviderAsync(string resourceProviderName);

        Task<string> GetResourceProviderAsync(string subscriptionId, string resourceProviderName);

        Task<string> RegisterResourceProviderAsync(string resourceProviderName);

        Task<string> RegisterResourceProviderAsync(string subscriptionId, string resourceProviderName);

        Task<string> RegisterFeatureAsync(string resourceProviderName, string featureName);

        Task<string> RegisterFeatureAsync(string subscriptionId, string resourceProviderName, string featureName);
        #endregion

        #region Resource Group
        Task<IResourceGroup> GetOrCreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags, CancellationToken cancellationToken = default);

        Task<IResourceGroup> CreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags, CancellationToken cancellationToken = default);

        IResourceGroup CreateResourceGroup(Region location, string rgName, IDictionary<string, string> tags);

        Task<IResourceGroup> GetResourceGroupAsync(string rgName, CancellationToken cancellationToken = default);

        Task DeleteResourceGroupAsync(string rgName, bool noThrow = false, CancellationToken cancellationToken = default);

        void DeleteResourceGroup(string rgName, bool noThrow = false);

        Task DeleteResourceGroupWithTagAsync(string tagName, string tagValue, Func<IReadOnlyDictionary<string, string>, bool> tagsFilter = null, CancellationToken cancellationToken = default);

        Task DeleteResourceGroupWithPrefixAsync(string rgNamePrefix, CancellationToken cancellationToken = default);

        Task DeleteResourceGroupWithNamePartAsync(string rgNamePart, CancellationToken cancellationToken = default);
        #endregion Resource Group

        #region Storage Account
        Task<IStorageAccount> GetOrCreateStorageAccountAsync(
            Region location,
            string rgName,
            string storageAccountName,
            IDictionary<string, string> tags,
            string accessFromSubnetId = null,
            CancellationToken cancellationToken = default);

        Task<IStorageAccount> CreateStorageAccountAsync(
            Region location,
            string rgName,
            string storageAccountName,
            IDictionary<string, string> tags,
            string accessFromSubnetId = null,
            CancellationToken cancellationToken = default);

        Task<IStorageAccount> GetStorageAccountAsync(
            string rgName,
            string storageAccountName,
            CancellationToken cancellationToken = default);

        Task<IStorageAccount> FindStorageAccountAsync(
            string storageAccountName,
            string resourceGroupNamePrefix = null,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IStorageAccount>> ListStorageAccountAsync(
            string rgName,
            string namePrefix = null,
            CancellationToken cancellationToken = default);

        Task GrantBlobContributorAsync(
            string subscriptionId,
            string objectId,
            CancellationToken cancellationToken = default);

        Task GrantBlobContributorAsync(
            string subscriptionId,
            IIdentity msi,
            CancellationToken cancellationToken = default);

        Task GrantBlobContributorAsync(
            IResourceGroup rg,
            string objectId,
            CancellationToken cancellationToken = default);

        Task GrantBlobContributorAsync(
            IResourceGroup rg,
            IIdentity msi,
            CancellationToken cancellationToken = default);

        Task GrantBlobContributorAsync(
            IStorageAccount storageAccount,
            string objectId,
            CancellationToken cancellationToken = default);

        Task GrantBlobContributorAsync(
            IStorageAccount storageAccount,
            IIdentity msi,
            CancellationToken cancellationToken = default);

        Task GrantBlobContainerContributorAsync(
            IStorageAccount storageAccount,
            string containerName,
            string objectId,
            CancellationToken cancellationToken = default);

        Task GrantBlobContainerContributorAsync(
            IStorageAccount storageAccount,
            string containerName,
            IIdentity msi,
            CancellationToken cancellationToken = default);

        Task GrantBlobContainerReaderAsync(
            IStorageAccount storageAccount,
            string containerName,
            string objectId,
            CancellationToken cancellationToken = default);

        Task GrantBlobContainerReaderAsync(
            IStorageAccount storageAccount,
            string containerName,
            IIdentity msi,
            CancellationToken cancellationToken = default);

        Task GrantQueueContributorAsync(
            IResourceGroup rg,
            string objectId,
            CancellationToken cancellationToken = default);

        Task GrantQueueContributorAsync(
            IResourceGroup rg,
            IIdentity msi,
            CancellationToken cancellationToken = default);

        Task GrantQueueContributorAsync(
            IStorageAccount storageAccount,
            IIdentity msi,
            CancellationToken cancellationToken = default);

        Task GrantQueueContributorAsync(
            IStorageAccount storageAccount,
            string objectId,
            CancellationToken cancellationToken = default);

        Task DelegateStorageKeyOperationToKeyVaultAsync(IStorageAccount storageAccount, CancellationToken cancellationToken = default);

        Task DelegateStorageKeyOperationToKeyVaultAsync(IResourceGroup rg, CancellationToken cancellationToken = default);
        #endregion Storage Account

        #region Network
        Task<INetworkSecurityGroup> GetNSGAsync(string rgName, string nsgName, CancellationToken cancellationToken = default);

        Task<INetworkSecurityGroup> GetOrCreateDefaultNSGAsync(
            Region location,
            string rgName,
            string nsgName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default);

        Task<INetwork> GetOrCreateVNetAsync(
            Region location,
            string rgName,
            string vnetName,
            IDictionary<string, string> tags,
            string nsgId = null,
            CancellationToken cancellationToken = default);

        Task<INetwork> CreateVNetAsync(
            Region location,
            string rgName,
            string vnetName,
            IDictionary<string, string> tags,
            string nsgId = null,
            CancellationToken cancellationToken = default);

        Task<INetwork> GetVNetAsync(string rgName, string vnetName, CancellationToken cancellationToken = default);

        Task<ISubnet> CreateNewSubnetAsync(
            INetwork vnet,
            string subnetName,
            string nsgId = null,
            CancellationToken cancellationToken = default);

        Task<ISubnet> GetSubnetAsync(string subnetId, CancellationToken cancellationToken = default);

        Task<IPublicIPAddress> GetOrCreatePublicIPAsync(
            Region location,
            string rgName,
            string pipName,
            IDictionary<string, string> tags,
            PublicIPSkuType skuType = null,
            CancellationToken cancellationToken = default);

        Task<IPublicIPAddress> CreatePublicIPAsync(
            Region location,
            string rgName,
            string pipName,
            IDictionary<string, string> tags,
            PublicIPSkuType skuType = null,
            CancellationToken cancellationToken = default);

        Task<IPublicIPAddress> GetPublicIPAsync(
            string rgName,
            string pipName,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<IPublicIPAddress>> ListPublicIPAsync(
            string rgName,
            string namePrefix = null,
            CancellationToken cancellationToken = default);

        Task<ITrafficManagerProfile> GetOrCreateTrafficManagerAsync(
            string rgName,
            string tmName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default);

        Task<ITrafficManagerProfile> CreateTrafficManagerAsync(
            string rgName,
            string tmName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default);

        Task<ITrafficManagerProfile> GetTrafficManagerAsync(string tmId, CancellationToken cancellationToken = default);

        Task<ITrafficManagerProfile> GetTrafficManagerAsync(string rgName, string tmName, CancellationToken cancellationToken = default);

        Task<IDnsZone> GetDNSZoneAsync(string rgName, string dnsName, CancellationToken cancellationToken = default);

        Task<IDnsZone> CreateDNSZoneAsync(string rgName, string dnsName, IDictionary<string, string> tags, CancellationToken cancellationToken = default);
        #endregion

        #region CosmosDB
        Task<ICosmosDBAccount> CreateCosmosDBAsync(
            Region location,
            string rgName,
            string cosmosDBName,
            IDictionary<string, string> tags,
            ISubnet subnet = null,
            CancellationToken cancellationToken = default);

        Task<ICosmosDBAccount> GetCosmosDBAsync(string dbResourceId, CancellationToken cancellationToken = default);

        Task<ICosmosDBAccount> GetCosmosDBAsync(string rgName, string cosmosDBName, CancellationToken cancellationToken = default);

        Task<IEnumerable<ICosmosDBAccount>> ListCosmosDBAsync(string rgName, CancellationToken cancellationToken = default);
        #endregion CosmosDB

        #region Key Vault
        Task<IVault> GetOrCreateKeyVaultAsync(
            Region location,
            string rgName,
            string vaultName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default);

        Task<IVault> GetOrCreateKeyVaultAsync(
            Region location,
            string rgName,
            string vaultName,
            string accessibleFromIP,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default);

        Task<IVault> CreateKeyVaultAsync(
            Region location,
            string rgName,
            string vaultName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default);

        Task<IVault> GetKeyVaultAsync(string rgName, string vaultName, CancellationToken cancellationToken = default);

        Task<IVault> GetKeyVaultByIdAsync(string kvResourceId, CancellationToken cancellationToken = default);

        Task<IEnumerable<IVault>> ListKeyVaultAsync(string rgName, string namePrefix = null, CancellationToken cancellationToken = default);

        Task WithKeyVaultAccessFromNetworkAsync(
            IVault vault,
            string ipAddress,
            string subnetId,
            bool enableVNetFilter = true,
            bool removeExistingIPs = true,
            CancellationToken cancellationToken = default);

        Task WithKeyVaultAccessFromNetworkAsync(
            IVault vault,
            IEnumerable<string> ipList,
            IEnumerable<string> subnetList,
            bool enableVNetFilter = true,
            bool removeExistingIPs = true,
            CancellationToken cancellationToken = default);

        Task RemoveAccessPolicyAsync(string kvResourceId, string servicePrincipalObjectId, CancellationToken cancellationToken = default);

        Task GrantSelfKeyVaultAdminAccessAsync(IVault kv, CancellationToken cancellationToken = default);

        Task RemoveSelfKeyVaultAccessAsync(IVault kv, CancellationToken cancellationToken = default);
        #endregion Key Vault

        #region Redis Cache
        Task<IRedisCache> GetOrCreateRedisCacheAsync(
            Region location,
            string rgName,
            string redisCacheName,
            IDictionary<string, string> tags,
            IDictionary<string, string> redisConfig = null,
            CancellationToken cancellationToken = default);

        Task<IRedisCache> GetRedisCachesAsync(string rgName, string redisCacheName, CancellationToken cancellationToken = default);

        Task<IEnumerable<IRedisCache>> ListRedisCacheAsync(string rgName, CancellationToken cancellationToken = default);

        Task<IRedisCache> CreateRedisCacheAsync(
            Region location,
            string rgName,
            string redisCacheName,
            IDictionary<string, string> tags,
            IDictionary<string, string> redisConfig = null,
            CancellationToken cancellationToken = default);
        #endregion

        #region AKS
        Task<IKubernetesCluster> CreateAksClusterAsync(
            Region region,
            string rgName,
            string aksName,
            string rootUserName,
            string sshPublicKey,
            AKSInfo aksInfo,
            string outboundIPId,
            IDictionary<string, string> tags,
            ISubnet subnet = null,
            string agentPoolProfileName = "ap",
            bool supportAvailabilityZone = false,
            CancellationToken cancellationToken = default);

        Task<IKubernetesCluster> GetAksClusterAsync(string aksResourceId, CancellationToken cancellationToken = default);

        Task<IKubernetesCluster> GetAksClusterAsync(string rgName, string aksName, CancellationToken cancellationToken = default);

        Task<IEnumerable<IKubernetesCluster>> ListAksClusterAsync(string rgName, CancellationToken cancellationToken = default);

        Task<string> GetAKSMIAsync(string rgName, string aksName, CancellationToken cancellationToken = default);

        Task<IEnumerable<IIdentity>> ListAKSMCMIAsync(string AKSRGName, string AKSName, Region location, CancellationToken cancellationToken = default);
        #endregion AKS

        #region Identity
        Task<IIdentity> GetOrCreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags);

        Task<IIdentity> CreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags);

        Task<IIdentity> GetMSIAsync(string rgName, string msiName);
        #endregion

        #region ACR
        Task<IRegistry> GetOrCreateACRAsync(Region location, string rgName, string acrName, IDictionary<string, string> tags);

        Task<IRegistry> GetACRAsync(string rgName, string acrName);

        Task<IEnumerable<IRegistry>> ListACRAsync(string rgName);
        #endregion

        #region Deployments
        Task<IDeployment> CreateDeploymentAsync(
            Region location,
            string rgName,
            string template,
            string templateParameters = null,
            bool noLogging = false,
            CancellationToken cancellationToken = default);
        #endregion

        #region Monitoring
        Task<string> GetOrCreateLogAnalyticsWorkspaceAsync(Region location, string rgName, string name, IDictionary<string, string> tags);

        Task<string> GetLogAnalyticsWorkspaceAsync(string rgName, string name);

        Task<IActionGroup> GetOrUpdateActionGroupAsync(string rgName, string name, string receiverName, string email);

        Task<IMetricAlert> GetOrUpdateMetricAlertAsync(string rgName, MetricAlertOptions alertOptions);
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
