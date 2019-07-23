//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Liftr.Fluent.Contracts.Geneva;
using Microsoft.Liftr.Fluent.Geneva;
using Microsoft.Rest.Azure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public class AzureClient : IAzureClient
    {
        public const string c_AspEnv = "ASPNETCORE_ENVIRONMENT";
        private readonly IAzure _azure;
        private readonly ILogger _logger;

        public AzureClient(IAzure azure, string clientId, string clientSecret, string servicePrincipalObjectId, ILogger logger)
        {
            _azure = azure;
            ClientId = clientId;
            ClientSecret = clientSecret;
            ServicePrincipalObjectId = servicePrincipalObjectId;
            _logger = logger;
        }

        public string ClientId { get; }

        public string ClientSecret { get; }

        public string ServicePrincipalObjectId { get; }

        #region Resource Group
        public async Task<IResourceGroup> CreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags)
        {
            _logger.Information("Creating a resource group with name: " + rgName);

            if (await _azure.ResourceGroups.ContainAsync(rgName))
            {
                var err = $"Resource Group with name '{rgName}' already existed.";
                _logger.Fatal(err);
                throw new InvalidOperationException(err);
            }

            var resourceGroup = await _azure
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
                return await _azure
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
            await _azure
                .ResourceGroups
                .DeleteByNameAsync(rgName);
            _logger.Information("Finished delete resource group with name: " + rgName);
        }

        public async Task DeleteResourceGroupWithTagAsync(string tagName, string tagValue, Func<IReadOnlyDictionary<string, string>, bool> tagsFilter = null)
        {
            var rgs = await _azure
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

        #region CosmosDB
        public async Task<(ICosmosDBAccount cosmosDBAccount, string mongoConnectionString)> CreateCosmosDBAsync(Region location, string rgName, string cosmosDBName, IDictionary<string, string> tags)
        {
            _logger.Information($"Creating a CosmosDB with name {cosmosDBName} ...");
            ICosmosDBAccount cosmosDBAccount = await _azure
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

        public async Task<IEnumerable<ICosmosDBAccount>> ListCosmosDBAsync(string rgName)
        {
            _logger.Information($"Listing CosmosDB in resource group {rgName} ...");
            return await _azure
                .CosmosDBAccounts
                .ListByResourceGroupAsync(rgName);
        }
        #endregion CosmosDB

        #region Key Vault
        public async Task<IVault> CreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags, string writerClientId)
        {
            _logger.Information($"Creating a Vault with name {vaultName} ...");

            // TODO: figure out how to remove Key Vault Access Policy of the management service principal.
            IVault vault = await _azure.Vaults
                        .Define(vaultName)
                        .WithRegion(location)
                        .WithExistingResourceGroup(rgName)
                        .DefineAccessPolicy()
                            .ForServicePrincipal(writerClientId)
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
                return await _azure.Vaults.GetByIdAsync(kvResourceId);
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
            return await _azure
                .Vaults
                .ListByResourceGroupAsync(rgName);
        }

        public async Task RemoveAccessPolicyAsync(string kvResourceId, string servicePrincipalObjectId)
        {
            var vault = await GetKeyVaultByIdAsync(kvResourceId) ?? throw new InvalidOperationException("Cannt find vault with resource Id: " + kvResourceId);
            await vault.Update().WithoutAccessPolicy(servicePrincipalObjectId).ApplyAsync();
            _logger.Information("Finished removing KeyVault {@kvResourceId} access policy of {@servicePrincipalObjectId}", kvResourceId, servicePrincipalObjectId);
        }
        #endregion Key Vault

        #region Web App
        public async Task<IWebApp> CreateWebAppAsync(Region location, string rgName, string webAppName, IDictionary<string, string> tags, PricingTier tier, string aspNetEnv)
        {
            tags = new Dictionary<string, string>(tags);
            tags[c_AspEnv] = aspNetEnv;

            _logger.Information($"Creating an App Service Plan with name {webAppName} ...");

            var webApp = await _azure.WebApps
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
            return await _azure
                .WebApps
                .ListByResourceGroupAsync(rgName);
        }

        public async Task<IAppServicePlan> GetAppServicePlanByIdAsync(string planResourceId)
        {
            try
            {
                _logger.Information($"Getting App Service Plan with resource Id {planResourceId} ...");
                return await _azure
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
                return await _azure
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

            var deployment = await _azure.Deployments
                .Define(deploymentName)
                .WithExistingResourceGroup(rgName)
                .WithTemplate(template)
                .WithParameters(templateParameters)
                .WithMode(DeploymentMode.Incremental)
                .CreateAsync();

            _logger.Information($"Finished the ARM deployment with name {deploymentName} ...");

            return deployment;
        }
        #endregion
    }
}
