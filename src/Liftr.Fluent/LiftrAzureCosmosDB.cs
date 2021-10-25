//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal partial class LiftrAzure
    {
        #region CosmosDB
        public async Task<ICosmosDBAccount> CreateCosmosDBAsync(
            Region location,
            string rgName,
            string cosmosDBName,
            IDictionary<string, string> tags,
            ISubnet subnet = null,
            bool? isZoneRedundant = null,
            CancellationToken cancellationToken = default)
        {
            var cosmosDBAccount = await GetCosmosDBAsync(rgName, cosmosDBName, cancellationToken);
            if (cosmosDBAccount == null)
            {
                var helper = new CosmosDBHelper(_logger);
                cosmosDBAccount = await helper.CreateCosmosDBAsync(this, location, rgName, cosmosDBName, tags, isZoneRedundant, cancellationToken);

                if (subnet != null)
                {
                    cosmosDBAccount = await cosmosDBAccount.Update().WithVirtualNetworkRule(subnet.Parent.Id, subnet.Name).ApplyAsync(cancellationToken);
                }

                _logger.Information($"Created CosmosDB with name {cosmosDBName}");
            }

            return cosmosDBAccount;
        }

        public Task<ICosmosDBAccount> GetCosmosDBAsync(string dbResourceId, CancellationToken cancellationToken = default)
        {
            _logger.Information("Getting CosmosDB with id '{dbResourceId}' ...", dbResourceId);
            return FluentClient
                .CosmosDBAccounts
                .GetByIdAsync(dbResourceId, cancellationToken);
        }

        public Task<ICosmosDBAccount> GetCosmosDBAsync(string rgName, string cosmosDBName, CancellationToken cancellationToken = default)
        {
            _logger.Information("Getting CosmosDB in rg '{rgName}' with name '{cosmosDBName}' ...", rgName, cosmosDBName);
            return FluentClient
                .CosmosDBAccounts
                .GetByResourceGroupAsync(rgName, cosmosDBName, cancellationToken);
        }

        public async Task<IEnumerable<ICosmosDBAccount>> ListCosmosDBAsync(string rgName, CancellationToken cancellationToken = default)
        {
            _logger.Information($"Listing CosmosDB in resource group {rgName} ...");
            return await FluentClient
                .CosmosDBAccounts
                .ListByResourceGroupAsync(rgName, loadAllPages: true, cancellationToken: cancellationToken);
        }
        #endregion CosmosDB
    }
}
