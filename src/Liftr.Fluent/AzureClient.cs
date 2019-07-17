//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public class AzureClient : IAzureClient
    {
        public const string c_AspEnv = "ASPNETCORE_ENVIRONMENT";
        private readonly IAzure _azure;
        private readonly ILogger _logger;

        public AzureClient(IAzure azure, string clientId, string clientSecret, ILogger logger)
        {
            _azure = azure;
            ClientId = clientId;
            ClientSecret = clientSecret;
            _logger = logger;
        }

        public string ClientId { get; }

        public string ClientSecret { get; }

        public HttpClient KeyVaultHttpClient => ((KeyVaultManagementClient)_azure.Vaults.Manager.Inner).HttpClient;

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
                            .AllowSecretPermissions(SecretPermissions.Set)
                            .Attach()
                        .WithTags(tags)
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
                        .WithTags(tags)
                        .WithAppSetting(c_AspEnv, aspNetEnv)
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
        #endregion Web App
    }
}
