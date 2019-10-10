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
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts.Geneva;
using Microsoft.Liftr.Fluent.Geneva;
using Microsoft.Rest.Azure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
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
        private readonly AzureCredentials _credentials;

        public LiftrAzure(AzureCredentials credentials, IAzure fluentClient, IAuthenticated authenticated, ILogger logger)
        {
            _credentials = credentials;
            FluentClient = fluentClient;
            Authenticated = authenticated;
            _logger = logger;
        }

        public IAzure FluentClient { get; }

        public IAuthenticated Authenticated { get; }

        #region Resource Group
        public async Task<IResourceGroup> CreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags)
        {
            _logger.Information("Creating a resource group with name: " + rgName);

            if (await FluentClient.ResourceGroups.ContainAsync(rgName))
            {
                var err = $"Resource Group with name '{rgName}' already existed.";
                _logger.Warning(err);
                throw new DuplicateNameException(err);
            }

            var resourceGroup = await FluentClient
                .ResourceGroups
                .Define(rgName)
                .WithRegion(location)
                .WithTags(tags)
                .CreateAsync();

            _logger.Information("Created a resource group with name: " + rgName);

            return resourceGroup;
        }

        public async Task<IResourceGroup> GetResourceGroupAsync(string rgName)
        {
            try
            {
                _logger.Information("Getting resource group with name: " + rgName);
                return await FluentClient
                .ResourceGroups
                .GetByNameAsync(rgName);
            }
            catch (CloudException ex) when (ex.Message.Contains("could not be found"))
            {
                _logger.Information(ex, ex.Message);
                return null;
            }
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

        #region Traffic Manager
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
        public async Task<IVault> CreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags, string adminSPNClientId)
        {
            _logger.Information("Creating a Vault with name {vaultName}, adminSPNClientId {adminSPNClientId} ...", vaultName, adminSPNClientId);

            if (!Guid.TryParse(adminSPNClientId, out _))
            {
                var errMsg = "The input kv admin client id is not in a valid Guid format.";
                var ex = new InvalidOperationException(errMsg);
                _logger.Error(ex, errMsg);
                throw ex;
            }

            // TODO: figure out how to remove Key Vault Access Policy of the management service principal.
            IVault vault = await FluentClient.Vaults
                        .Define(vaultName)
                        .WithRegion(location)
                        .WithExistingResourceGroup(rgName)
                        .DefineAccessPolicy()
                            .ForServicePrincipal(adminSPNClientId)
                            .AllowSecretAllPermissions()
                            .AllowCertificateAllPermissions()
                            .Attach()
                        .WithTags(tags)
                        .WithDeploymentEnabled()
                        .WithTemplateDeploymentEnabled()
                        .CreateAsync();

            _logger.Information($"Created Vault with name {vaultName}");

            return vault;
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

        #region Web App
        public async Task<IWebApp> CreateWebAppAsync(Region location, string rgName, string webAppName, IDictionary<string, string> tags, PricingTier tier, string aspNetEnv)
        {
            tags = new Dictionary<string, string>(tags);
            tags[c_AspEnv] = aspNetEnv;

            _logger.Information($"Creating an App Service Plan with name {webAppName} ...");

            var webApp = await FluentClient.WebApps
                        .Define(webAppName)
                        .WithRegion(location)
                        .WithExistingResourceGroup(rgName)
                        .WithNewWindowsPlan(tier)
                        .WithPlatformArchitecture(PlatformArchitecture.X64) // For Geneva monitoring.
                        .WithWebAppAlwaysOn(alwaysOn: true)
                        .WithTags(tags)
                        .WithAppSetting(c_AspEnv, aspNetEnv)
                        .WithAppSetting("WEBSITE_FIRST_PARTY_ID", "AntMDS") // For Geneva monitoring.
                        .WithSystemAssignedManagedServiceIdentity()
                        .CreateAsync();

            _logger.Information($"Created App Service Plan with name {webAppName}");

            return webApp;
        }

        public async Task<IEnumerable<IWebApp>> ListWebAppAsync(string rgName)
        {
            _logger.Information($"Listing WebApp in resource group {rgName} ...");
            return await FluentClient
                .WebApps
                .ListByResourceGroupAsync(rgName);
        }

        public async Task<IAppServicePlan> GetAppServicePlanByIdAsync(string planResourceId)
        {
            try
            {
                _logger.Information($"Getting App Service Plan with resource Id {planResourceId} ...");
                return await FluentClient
                    .AppServices
                    .AppServicePlans
                    .GetByIdAsync(planResourceId);
            }
            catch (CloudException ex) when (ex.Message.Contains("could not be found"))
            {
                _logger.Information(ex, ex.Message);
                return null;
            }
        }

        public async Task<IWebApp> GetWebAppWithIdAsync(string resourceId)
        {
            try
            {
                _logger.Information($"Getting Web App with resource Id {resourceId} ...");
                return await FluentClient
                    .WebApps
                    .GetByIdAsync(resourceId);
            }
            catch (CloudException ex) when (ex.Message.Contains("could not be found"))
            {
                _logger.Information(ex, ex.Message);
                return null;
            }
        }

        public async Task<IAppServiceCertificate> UploadCertificateToWebAppAsync(string webAppId, string certName, byte[] pfxByteArray)
        {
            var webApp = await GetWebAppWithIdAsync(webAppId) ?? throw new InvalidOperationException("Cannot find web app with id: " + webAppId);
            _logger.Information($"Creating an App Service Certificate with name {certName} ...");
            var cert = await webApp.Manager
                .AppServiceCertificates
                .Define(certName)
                .WithRegion(webApp.Region)
                .WithExistingResourceGroup(webApp.ResourceGroupName)
                .WithPfxByteArray(pfxByteArray)
                .WithPfxPassword(string.Empty)
                .CreateAsync();
            _logger.Information($"Created App Service Certificate with name {certName}");

            return cert;
        }

        public async Task DeployGenevaToAppServicePlanAsync(string appServicePlanResoureId, GenevaOptions genevaOptions, string based64EncodedPFX)
        {
            var appServicePlan = await GetAppServicePlanByIdAsync(appServicePlanResoureId);
            if (appServicePlan == null)
            {
                var ex = new InvalidOperationException($"Cannot find the app serivce plan with Resource Id {appServicePlanResoureId}. Please make sure it exist before deploying Geneva to it.");
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            var jsonConfig = AntaresHelper.AssembleConfigJson(genevaOptions, appServicePlan.Region);
            _logger.Information("Generated Antares json config: {@AntMDSConfig}", jsonConfig);

            var jsonConfigTemplate = AntaresHelper.GenerateAntJsonConfigTemplate(appServicePlan.Region, appServicePlan.Name, jsonConfig);
            _logger.Information("Putting ConfigJson file to the App Service plan with name {@AppServicePlanName} ...", appServicePlan.Name);
            var jsonDeployment = await CreateDeploymentAsync(appServicePlan.Region, appServicePlan.ResourceGroupName, jsonConfigTemplate);
            _logger.Information("Finished putting ConfigJson file to the App Service plan with name {@AppServicePlanName}", appServicePlan.Name);

            var xmlConfigTemplate = AntaresHelper.GenerateAntXMLConfigTemplate(appServicePlan.Region, appServicePlan.Name);
            _logger.Information("Putting XMLConfig file to the App Service plan with name {@AppServicePlanName} ...", appServicePlan.Name);
            var xmlDeployment = await CreateDeploymentAsync(appServicePlan.Region, appServicePlan.ResourceGroupName, xmlConfigTemplate);
            _logger.Information("Finished putting XMLConfig file to the App Service plan with name {@AppServicePlanName}", appServicePlan.Name);

            var gcsCertTemplate = AntaresHelper.GenerateGCSCertTemplate(appServicePlan.Region, appServicePlan.Name, based64EncodedPFX);
            _logger.Information("Putting GCS cert to the App Service plan with name {@AppServicePlanName} ...", appServicePlan.Name);
            var certDeployment = await CreateDeploymentAsync(appServicePlan.Region, appServicePlan.ResourceGroupName, gcsCertTemplate, templateParameters: null, noLogging: true);
            _logger.Information("Finished GCS cert to the App Service plan with name {@AppServicePlanName}", appServicePlan.Name);

            var gcsCertPSWDTemplate = AntaresHelper.GenerateGCSCertPSWDTemplate(appServicePlan.Region, appServicePlan.Name);
            _logger.Information("Putting GCS cert PSWD to the App Service plan with name {@AppServicePlanName} ...", appServicePlan.Name);
            var certPSWDDeployment = await CreateDeploymentAsync(appServicePlan.Region, appServicePlan.ResourceGroupName, gcsCertPSWDTemplate);
            _logger.Information("Finished GCS cert PSWD to the App Service plan with name {@AppServicePlanName}", appServicePlan.Name);
        }

        #endregion Web App

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
                var error = await DeploymentExtensions.GetDeploymentErrorDetailsAsync(FluentClient.SubscriptionId, rgName, deploymentName, _credentials);
                _logger.Error("ARM deployment with name {@deploymentName} Failed with Error: {@DeploymentError}", deploymentName, error);
                throw new ARMDeploymentFailureException("ARM deployment failed", ex) { Details = error };
            }
        }
        #endregion
    }
}
