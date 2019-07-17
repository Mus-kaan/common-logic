//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Liftr.Fluent.Contracts;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class InftrastructureV1
    {
        private readonly IAzureClient _azure;
        private readonly ILogger _logger;

        public InftrastructureV1(IAzureClient azureClient, ILogger logger)
        {
            _azure = azureClient;
            _logger = logger;
        }

        public async Task<(IResourceGroup dataResourceGroup, IResourceGroup computeResourceGroup)> CreateDataAndComputeAsync(InfraV1Options options, NamingContext context = null)
        {
            options.CheckValid();

            if (context == null)
            {
                context = new NamingContext(options.PartnerName, options.ShortPartnerName, options.Environment, options.Location);
            }

            (var dataRG, string keyVaultResourceId) = await CreateDataResourceGroupAsync(options.DataCoreName, context, options.CosmosSecreteName);
            var computeRG = await CreateComputeGroupAsync(options.ComputeCoreName, context, options.WebAppTier, options.AspNetEnv, keyVaultResourceId);

            return (dataRG, computeRG);
        }

        public async Task<(IResourceGroup rg, string keyVaultResourceId)> CreateDataResourceGroupAsync(string coreName, NamingContext context, string cosmosSecreteName)
        {
            var rgName = context.ResourceGroupName(coreName);
            var kvName = context.KeyVaultName(coreName);
            var cosmosName = context.CosmosDBName(coreName);

            _logger.Information("Creating Resource Group ...");
            var rg = await _azure.CreateResourceGroupAsync(context.Location, rgName, context.Tags);
            _logger.Information("Created {@ResourceGroup}", rg);

            _logger.Information("Creating Key Vault ...");
            var kv = await _azure.CreateKeyVaultAsync(context.Location, rgName, kvName, context.Tags, _azure.ClientId);
            _logger.Information("Created {@KeyVault}", kv);

            _logger.Information("Creating CosmosDB ...");
            (var db, string connectionString) = await _azure.CreateCosmosDBAsync(context.Location, rgName, cosmosName, context.Tags);
            _logger.Information("Created {@CosmosDB}", db);

            _logger.Information("Puting the CosmosDB Connection String in the key vault ...");
            IKeyVaultClient keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) =>
                {
                    var authContext = new AuthenticationContext(authority, TokenCache.DefaultShared);
                    var result = await authContext.AcquireTokenAsync(resource, new ClientCredential(_azure.ClientId, _azure.ClientSecret));
                    return result.AccessToken;
                }), _azure.KeyVaultHttpClient);

            await keyVaultClient.SetSecretAsync(kv.VaultUri, cosmosSecreteName, connectionString, context.Tags);
            _logger.Information("Finished creating data resource group: {@ResourceGroup}", rg);

            return (rg, kv.Id);
        }

        public async Task<IResourceGroup> CreateComputeGroupAsync(string coreName, NamingContext context, PricingTier tier, string aspNetEnv, string kvResourceId)
        {
            var rgName = context.ResourceGroupName(coreName);
            var webAppName = context.WebAppName(coreName);

            _logger.Information("Creating Resource Group ...");
            var rg = await _azure.CreateResourceGroupAsync(context.Location, rgName, context.Tags);
            _logger.Information("Created {@ResourceGroup}", rg);

            _logger.Information("Creating Web App ...");
            var webApp = await _azure.CreateWebAppAsync(context.Location, rgName, webAppName, context.Tags, tier, aspNetEnv);
            _logger.Information("Created {@WebApp}", webApp);

            _logger.Information("Updating key vault to allow the web app to access ...");
            var kv = await _azure.GetKeyVaultByIdAsync(kvResourceId);
            if (kv == null)
            {
                var errMsg = $"Cannot find an existing Key Vault with the resource Id: {kvResourceId}";
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            kv = await kv.Update()
                .DefineAccessPolicy()
                    .ForObjectId(webApp.SystemAssignedManagedServiceIdentityPrincipalId)
                    .AllowSecretPermissions(SecretPermissions.List, SecretPermissions.Get)
                    .Attach()
                 .ApplyAsync();

            _logger.Information("Finished creating compute resource group: {@ResourceGroup}", rg);

            return rg;
        }
    }
}
