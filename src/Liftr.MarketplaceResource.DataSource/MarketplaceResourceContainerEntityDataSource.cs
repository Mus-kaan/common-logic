﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.MarketplaceResource.DataSource.Interfaces;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Microsoft.Liftr.MarketplaceResource.DataSource
{
    public class MarketplaceResourceContainerEntityDataSource : ResourceEntityDataSource<MarketplaceResourceContainerEntity>, IMarketplaceResourceContainerEntityDataSource
    {
        public MarketplaceResourceContainerEntityDataSource(IMongoCollection<MarketplaceResourceContainerEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
            : base(collection, rateLimiter, timeSource)
        {
        }

        public async Task<IMarketplaceResourceContainerEntity> GetEntityForMarketplaceSubscriptionAsync(MarketplaceSubscription marketplaceSubscription)
        {
            if (marketplaceSubscription is null)
            {
                throw new System.ArgumentNullException(nameof(marketplaceSubscription));
            }

            var builder = Builders<MarketplaceResourceContainerEntity>.Filter;
            var filter = builder.Eq(u => u.MarketplaceSaasResource.MarketplaceSubscription, marketplaceSubscription);

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync<MarketplaceResourceContainerEntity>(filter);
                return await cursor.FirstOrDefaultAsync();
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
    }
}
