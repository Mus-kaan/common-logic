//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.MarketplaceResource.DataSource.Interfaces;
using Liftr.MarketplaceResource.DataSource.Models;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Liftr.MarketplaceResource.DataSource
{
    public class MarketplaceResourceEntityDataSource : ResourceEntityDataSource<MarketplaceResourceEntity>, IMarketplaceResourceEntityDataSource
    {
        public MarketplaceResourceEntityDataSource(IMongoCollection<MarketplaceResourceEntity> collection, ITimeSource timeSource)
            : base(collection, timeSource)
        {
        }

        public async Task<IMarketplaceResourceEntity> GetEntityForMarketplaceSubscriptionAsync(MarketplaceSubscription marketplaceSubscription)
        {
            var builder = Builders<MarketplaceResourceEntity>.Filter;
            var filter = builder.Eq(u => u.MarketplaceSubscription, marketplaceSubscription);
            var cursor = await _collection.FindAsync<MarketplaceResourceEntity>(filter);
            return await cursor.FirstOrDefaultAsync();
        }
    }
}
