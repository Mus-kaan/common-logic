//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts.Geneva;
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
        #endregion Storage Account

        #region Traffic Manager
        Task<ITrafficManagerProfile> CreateTrafficManagerAsync(string rgName, string tmName, IDictionary<string, string> tags);

        Task<ITrafficManagerProfile> GetTrafficManagerAsync(string tmId);
        #endregion

        #region CosmosDB
        Task<(ICosmosDBAccount cosmosDBAccount, string mongoConnectionString)> CreateCosmosDBAsync(Region location, string rgName, string cosmosDBName, IDictionary<string, string> tags);

        Task<ICosmosDBAccount> GetCosmosDBAsync(string dbResourceId);

        Task<IEnumerable<ICosmosDBAccount>> ListCosmosDBAsync(string rgName);
        #endregion CosmosDB

        #region Key Vault
        Task<IVault> GetOrCreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags, string adminSPNClientId);

        Task<IVault> CreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags, string adminSPNClientId);

        Task<IVault> GetKeyVaultAsync(string rgName, string vaultName);

        Task<IVault> GetKeyVaultByIdAsync(string kvResourceId);

        Task<IEnumerable<IVault>> ListKeyVaultAsync(string rgName);

        Task RemoveAccessPolicyAsync(string kvResourceId, string servicePrincipalObjectId);
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

        #region Web App
        Task<IWebApp> CreateWebAppAsync(Region location, string rgName, string webAppName, IDictionary<string, string> tags, PricingTier tier, string aspNetEnv);

        Task<IEnumerable<IWebApp>> ListWebAppAsync(string rgName);

        Task<IAppServicePlan> GetAppServicePlanByIdAsync(string planResourceId);

        Task<IWebApp> GetWebAppWithIdAsync(string resourceId);

        Task<IAppServiceCertificate> UploadCertificateToWebAppAsync(string webAppId, string certName, byte[] pfxByteArray);

        Task DeployGenevaToAppServicePlanAsync(string appServicePlanResoureId, GenevaOptions genevaOptions, string based64EncodedPFX);
        #endregion Web App

        #region Identity
        Task<IIdentity> CreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags);

        Task<IIdentity> GetMSIAsync(string msiId);
        #endregion

        #region Deployments
        Task<IDeployment> CreateDeploymentAsync(Region location, string rgName, string template, string templateParameters = null, bool noLogging = false);
        #endregion
    }
}
