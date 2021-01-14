//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Liftr.MarketplaceRelationship.DataSource
{
    public interface IMarketplaceRelationshipEntityDataSource<TEntity> where TEntity : MarketplaceRelationshipEntity
    {
        /// <summary>
        /// List all the relationship entities for the marketplace subscription.
        /// </summary>
        Task<IEnumerable<TEntity>> ListAsync(MarketplaceSubscription marketplaceSubscription, bool showActiveOnly = true);
    }
}
