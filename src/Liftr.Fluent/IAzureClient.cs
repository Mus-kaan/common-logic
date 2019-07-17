//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public interface IAzureClient
    {
        string ClientId { get; }

        string ClientSecret { get; }

        HttpClient KeyVaultHttpClient { get; }

        #region Resource Group
        Task<IResourceGroup> CreateResourceGroupAsync(Region location, string rgName, IDictionary<string, string> tags);

        Task<IResourceGroup> GetResourceGroupAsync(string rgName);

        Task DeleteResourceGroupAsync(string rgName);
        #endregion Resource Group

        #region CosmosDB
        Task<(ICosmosDBAccount cosmosDBAccount, string mongoConnectionString)> CreateCosmosDBAsync(Region location, string rgName, string cosmosDBName, IDictionary<string, string> tags);

        Task<IEnumerable<ICosmosDBAccount>> ListCosmosDBAsync(string rgName);
        #endregion CosmosDB

        #region Key Vault
        Task<IVault> CreateKeyVaultAsync(Region location, string rgName, string vaultName, IDictionary<string, string> tags, string writerClientId);

        Task<IVault> GetKeyVaultByIdAsync(string kvResourceId);

        Task<IEnumerable<IVault>> ListKeyVaultAsync(string rgName);
        #endregion Key Vault

        #region Web App
        Task<IWebApp> CreateWebAppAsync(Region location, string rgName, string webAppName, IDictionary<string, string> tags, PricingTier tier, string aspNetEnv);

        Task<IEnumerable<IWebApp>> ListWebAppAsync(string rgName);
        #endregion Web App
    }
}
