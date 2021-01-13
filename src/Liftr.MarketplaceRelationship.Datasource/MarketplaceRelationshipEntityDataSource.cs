//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.MarketplaceRelationship.DataSource;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.MarketplaceRelationship.DataSource
{
    public class MarketplaceRelationshipEntityDataSource<TEntity> : ResourceEntityDataSource<TEntity>, IMarketplaceRelationshipEntityDataSource<TEntity> where TEntity : MarketplaceRelationshipEntity
    {
        public MarketplaceRelationshipEntityDataSource(IMongoCollection<TEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
            : base(collection, rateLimiter, timeSource)
        {
        }

        public virtual async Task<IEnumerable<TEntity>> ListAsync(MarketplaceSubscription marketplaceSubscription)
        {
            var builder = Builders<TEntity>.Filter;
            var filter = builder.Eq(u => u.MarketplaceSubscription, marketplaceSubscription);

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = _collection.Find(filter);
                var entities = await cursor.ToListAsync();

                return entities;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
    }
}
