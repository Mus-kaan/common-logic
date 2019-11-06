//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
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
using System.Linq;
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
        public const string c_AspEnv = "ASPNETCORE_ENVIRONMENT";
        private readonly ILogger _logger;

        public LiftrAzure(
            string tenantId,
            string clientId,
            AzureCredentials credentials,
            IAzure fluentClient,
            IAuthenticated authenticated,
            ILogger logger)
        {
            TenantId = tenantId;
            ClientId = clientId;
            AzureCredentials = credentials;
            FluentClient = fluentClient;
            Authenticated = authenticated;
            _logger = logger;
        }

        public string TenantId { get; }

        public string ClientId { get; }

        public IAzure FluentClient { get; }

        public IAuthenticated Authenticated { get; }

        public AzureCredentials AzureCredentials { get; }

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
                    _logger.Information("Retrieved the Resource Group with Id:{resourceId}", rg.Id);
                    return rg;
                }
            }
            catch (CloudException ex) when (ex.Message.Contains("could not be found"))
            {
            }

            _logger.Information("Cannot find resource group with name: {rgName}.", rgName);
            return null;
        }

        public async Task DeleteResourceGroupAsync(string rgName)
        {
            _logger.Information("Deleteing resource group with name: " + rgName);
            await FluentClient
                .ResourceGroups
                .DeleteByNameAsync(rgName);
            _logger.Information("Finished delete resource group with name: " + rgName);
        }

        public async Task DeleteResourceGroupWithTagAsync(string tagName, string tagValue, Func<IReadOnlyDictionary<string, string>, bool> tagsFilter = null)
        {
            var rgs = await FluentClient
                .ResourceGroups
                .ListByTagAsync(tagName, tagValue);
            _logger.Information("There are {@rgCount} with tagName {@tagName} and {@tagValue}.", rgs.Count(), tagName, tagValue);

            List<Task> tasks = new List<Task>();
            foreach (var rg in rgs)
            {
                if (tagsFilter == null || tagsFilter.Invoke(rg.Tags) == true)
                {
                    tasks.Add(DeleteResourceGroupAsync(rg.Name));
                }
            }

            await Task.WhenAll(tasks);
        }
        #endregion Resource Group

        #region Storage Account
        public async Task<IStorageAccount> GetOrCreateStorageAccountAsync(Region location, string rgName, string storageAccountName, IDictionary<string, string> tags)
        {
            var stor = await GetStorageAccountAsync(rgName, storageAccountName);

            if (stor == null)
            {
                stor = await CreateStorageAccountAsync(location, rgName, storageAccountName, tags);
            }

            return stor;
        }

        public async Task<IStorageAccount> CreateStorageAccountAsync(Region location, string rgName, string storageAccountName, IDictionary<string, string> tags)
        {
            _logger.Information("Creating storage account with name {storageAccountName} in {rgName}", storageAccountName, rgName);

            var storageAccount = await FluentClient.StorageAccounts
                .Define(storageAccountName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithOnlyHttpsTraffic()
                .WithTags(tags)
                .CreateAsync();

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

        public async Task<IEnumerable<IStorageAccount>> ListStorageAccountAsync(string rgName)
        {
            _logger.Information("Listing storage accounts in rgName: {rgName} ...", rgName);

            var accounts = await FluentClient
                .StorageAccounts
                .ListByResourceGroupAsync(rgName);

            _logger.Information("Found {cnt} storage accounts in rgName: {rgName} ...", accounts.Count(), rgName);
            return accounts.ToList();
        }
        #endregion Storage Account

        #region Network
        public async Task<INetwork> GetOrCreateVNetAsync(Region location, string rgName, string vnetName, string addressSpaceCIDR, IDictionary<string, string> tags)
        {
            var vnet = await GetVNetAsync(rgName, vnetName);
            if (vnet == null)
            {
                vnet = await CreateVNetAsync(location, rgName, vnetName, addressSpaceCIDR, tags);
            }

            return vnet;
        }

        public async Task<INetwork> CreateVNetAsync(Region location, string rgName, string vnetName, string addressSpaceCIDR, IDictionary<string, string> tags)
        {
            _logger.Information("Start creating VNet with name: {vnetName} in RG: {rgName} with Addresses: {addressSpaceCIDR} ...", vnetName, rgName, addressSpaceCIDR);

            var vnet = await FluentClient
                .Networks
                .Define(vnetName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithTags(tags)
                .WithAddressSpace(addressSpaceCIDR)
                .CreateAsync();

            _logger.Information("Created VNet with resourceId: {resourceId}", vnet.Id);
            return vnet;
        }

        public async Task<INetwork> GetVNetAsync(string rgName, string vnetName)
        {
            _logger.Information("Start getting VNet with name: {vnetName} in RG: {rgName} ...", vnetName, rgName);

            var vnet = await FluentClient
                .Networks
                .GetByResourceGroupAsync(rgName, vnetName);

            return vnet;
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
                .WithTags(tags)
                .CreateAsync();

            _logger.Information("Created Publib IP address with resourceId: {resourceId}", pip.Id);
            return pip;
        }

        public async Task<IPublicIPAddress> GetPublicIPAsync(string rgName, string pipName)
        {
            _logger.Information("Start getting Public IP with name: {pipName} ...", pipName);

            var pip = await FluentClient
                .PublicIPAddresses
                .GetByResourceGroupAsync(rgName, pipName);

            return pip;
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
        #endregion

        #region CosmosDB
        public async Task<(ICosmosDBAccount cosmosDBAccount, string mongoConnectionString)> CreateCosmosDBAsync(Region location, string rgName, string cosmosDBName, IDictionary<string, string> tags)
        {
            _logger.Information($"Creating a CosmosDB with name {cosmosDBName} ...");
            ICosmosDBAccount cosmosDBAccount = await FluentClient
                .CosmosDBAccounts
                .Define(cosmosDBName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithDataModelMongoDB()
                .WithStrongConsistency()
                .WithTags(tags)
                .CreateAsync();

            _logger.Information($"Created CosmosDB with name {cosmosDBName}");

            _logger.Information("Get the MongoDB connection string");
            var databaseAccountListConnectionStringsResult = await cosmosDBAccount.ListConnectionStringsAsync();
            var mongoConnectionString = databaseAccountListConnectionStringsResult.ConnectionStrings[0].ConnectionString;

            return (cosmosDBAccount, mongoConnectionString);
        }

        public async Task<ICosmosDBAccount> GetCosmosDBAsync(string dbResourceId)
        {
            return await FluentClient
                .CosmosDBAccounts
                .GetByIdAsync(dbResourceId);
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
            catch (CloudException ex) when (ex.Message.Contains("could not be found"))
            {
                _logger.Information(ex, ex.Message);
                return null;
            }
        }

        public async Task<IEnumerable<IVault>> ListKeyVaultAsync(string rgName)
        {
            _logger.Information($"Listing KeyVault in resource group {rgName} ...");
            return await FluentClient
                .Vaults
                .ListByResourceGroupAsync(rgName);
        }

        public async Task RemoveAccessPolicyAsync(string kvResourceId, string servicePrincipalObjectId)
        {
            if (string.IsNullOrEmpty(servicePrincipalObjectId))
            {
                throw new ArgumentNullException(nameof(servicePrincipalObjectId));
            }

            var vault = await GetKeyVaultByIdAsync(kvResourceId) ?? throw new InvalidOperationException("Cannt find vault with resource Id: " + kvResourceId);
            await vault.Update().WithoutAccessPolicy(servicePrincipalObjectId).ApplyAsync();
            _logger.Information("Finished removing KeyVault {@kvResourceId} access policy of {@servicePrincipalObjectId}", kvResourceId, servicePrincipalObjectId);
        }
        #endregion Key Vault

        #region Aks Cluster
        public async Task<IKubernetesCluster> CreateAksClusterAsync(
            Region region,
            string rgName,
            string aksName,
            string rootUserName,
            string sshPublicKey,
            string servicePrincipalClientId,
            string servicePrincipalSecret,
            ContainerServiceVirtualMachineSizeTypes vmSizeType,
            int vmCount,
            IDictionary<string, string> tags)
        {
            _logger.Information($"Creating a Kubernetes cluster with name {aksName} ...");

            var k8sCluster = await FluentClient.KubernetesClusters
                             .Define(aksName)
                             .WithRegion(region)
                             .WithExistingResourceGroup(rgName)
                             .WithLatestVersion()
                             .WithRootUsername(rootUserName)
                             .WithSshKey(sshPublicKey)
                             .WithServicePrincipalClientId(servicePrincipalClientId)
                             .WithServicePrincipalSecret(servicePrincipalSecret)
                             .DefineAgentPool("pool1")
                                 .WithVirtualMachineSize(vmSizeType)
                                 .WithAgentPoolVirtualMachineCount(vmCount)
                                 .Attach()
                             .WithDnsPrefix(aksName)
                             .WithTags(tags)
                             .CreateAsync();

            _logger.Information($"Created Kubernetes cluster with name {aksName}");
            return k8sCluster;
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
        #endregion Aks Cluster

        #region Identity
        public async Task<IIdentity> CreateMSIAsync(Region location, string rgName, string msiName, IDictionary<string, string> tags)
        {
            _logger.Information("Creating a MSI with name {msiName} ...", msiName);
            var msi = await FluentClient.Identities
                .Define(msiName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithTags(tags)
                .CreateAsync();
            _logger.Information("Created MSI with Id {ResourceId} ...", msi.Id);
            return msi;
        }

        public async Task<IIdentity> GetMSIAsync(string msiId)
        {
            _logger.Information("Getting MSI with Id {ResourceId} ...", msiId);
            return await FluentClient.Identities.GetByIdAsync(msiId);
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
    }
}
