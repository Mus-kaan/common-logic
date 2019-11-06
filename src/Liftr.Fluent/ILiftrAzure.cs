//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Microsoft.Azure.Management.Fluent.Azure;

namespace Microsoft.Liftr.Fluent
{
    public interface ILiftrAzure
    {
        IAzure FluentClient { get; }

        IAuthenticated Authenticated { get; }

        AzureCredentials AzureCredentials { get; }

        string TenantId { get; }

        string ClientId { get; }

        #region Resource Group
        Task<IResourceGroup> GetOrCreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags);

        Task<IResourceGroup> CreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags);

        Task<IResourceGroup> GetResourceGroupAsync(string rgName);

        Task DeleteResourceGroupAsync(string rgName);

        Task DeleteResourceGroupWithTagAsync(string tagName, string tagValue, Func<IReadOnlyDictionary<string, string>, bool> tagsFilter = null);
        #endregion Resource Group

        #region Storage Account
        Task<IStorageAccount> GetOrCreateStorageAccountAsync(Region location, string rgName, string storageAccountName, IDictionary<string, string> tags);

        Task<IStorageAccount> CreateStorageAccountAsync(Region location, string rgName, string storageAccountName, IDictionary<string, string> tags);

        Task<IStorageAccount> GetStorageAccountAsync(string rgName, string storageAccountName);

        Task<IEnumerable<IStorageAccount>> ListStorageAccountAsync(string rgName);
        #endregion Storage Account

        #region Network
        Task<INetwork> GetOrCreateVNetAsync(Region location, string rgName, string vnetName, string addressSpaceCIDR, IDictionary<string, string> tags);

        Task<INetwork> CreateVNetAsync(Region location, string rgName, string vnetName, string addressSpaceCIDR, IDictionary<string, string> tags);

        Task<INetwork> GetVNetAsync(string rgName, string vnetName);

        Task<IPublicIPAddress> GetOrCreatePublicIPAsync(Region location, string rgName, string pipName, IDictionary<string, string> tags);

        Task<IPublicIPAddress> CreatePublicIPAsync(Region location, string rgName, string pipName, IDictionary<string, string> tags);

        Task<IPublicIPAddress> GetPublicIPAsync(string rgName, string pipName);

        Task<ITrafficManagerProfile> CreateTrafficManagerAsync(string rgName, string tmName, IDictionary<string, string> tags);

        Task<ITrafficManagerProfile> GetTrafficManagerAsync(string tmId);
        #endregion

        #region CosmosDB
        Task<(ICosmosDBAccount cosmosDBAccount, string mongoConnectionString)> CreateCosmosDBAsync(Region location, string rgName, string cosmosDBName, IDictionary<string, string> tags);

        Task<ICosmosDBAccount> GetCosmosDBAsync(string dbResourceId);

        Task<IEnumerable<ICosmosDBAccount>> ListCosmosDBAsync(string rgName);
        #endregion CosmosDB

        #region Key Vault
        Task<IVault> GetOrCreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags);

        Task<IVault> CreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags);

        Task<IVault> GetKeyVaultAsync(string rgName, string vaultName);

        Task<IVault> GetKeyVaultByIdAsync(string kvResourceId);

        Task<IEnumerable<IVault>> ListKeyVaultAsync(string rgName);

        Task RemoveAccessPolicyAsync(string kvResourceId, string servicePrincipalObjectId);

        Task GrantSelfKeyVaultAdminAccessAsync(IVault kv);

        Task RemoveSelfKeyVaultAccessAsync(IVault kv);
        #endregion Key Vault

        #region Aks Cluster
        Task<IKubernetesCluster> CreateAksClusterAsync(
            Region region,
            string rgName,
            string aksName,
            string rootUserName,
            string sshPublicKey,
            string servicePrincipalClientId,
            string servicePrincipalSecret,
            ContainerServiceVirtualMachineSizeTypes vmSizeType,
            int vmCount,
            IDictionary<string, string> tags);

        Task<IKubernetesCluster> GetAksClusterAsync(string aksResourceId);

        Task<IEnumerable<IKubernetesCluster>> ListAksClusterAsync(string rgName);
        #endregion Aks Cluster

        #region Identity
        Task<IIdentity> GetOrCreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags);

        Task<IIdentity> CreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags);

        Task<IIdentity> GetMSIAsync(string rgName, string msiName);
        #endregion

        #region Deployments
        Task<IDeployment> CreateDeploymentAsync(Region location, string rgName, string template, string templateParameters = null, bool noLogging = false);
        #endregion
    }
}
