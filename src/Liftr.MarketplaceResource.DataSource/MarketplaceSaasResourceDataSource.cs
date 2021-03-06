//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.MarketplaceResource.DataSource
{
    public class MarketplaceSaasResourceDataSource : IMarketplaceSaasResourceDataSource
    {
        private readonly IMongoCollection<MarketplaceSaasResourceEntity> _collection;
        private readonly MongoWaitQueueRateLimiter _rateLimiter;
        private readonly ITimeSource _timeSource;

        public MarketplaceSaasResourceDataSource(IMongoCollection<MarketplaceSaasResourceEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
        {
            _collection = collection ?? throw new System.ArgumentNullException(nameof(collection));
            _rateLimiter = rateLimiter ?? throw new System.ArgumentNullException(nameof(rateLimiter));
            _timeSource = timeSource ?? throw new System.ArgumentNullException(nameof(timeSource));
        }

        public virtual async Task<MarketplaceSaasResourceEntity> AddAsync(MarketplaceSaasResourceEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await _rateLimiter.WaitAsync();
            try
            {
                entity.CreatedUTC = _timeSource.UtcNow;
                entity.LastModifiedUTC = _timeSource.UtcNow;
                entity.Active = true;
                await _collection.InsertOneAsync(entity);
                return entity;
            }
            catch (Exception ex) when (ex.IsMongoDuplicatedKeyException())
            {
                throw new DuplicatedKeyException(ex);
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<MarketplaceSaasResourceEntity> GetAsync(MarketplaceSubscription marketplaceSubscription)
        {
            var builder = Builders<MarketplaceSaasResourceEntity>.Filter;
            var filter = builder.Eq(u => u.MarketplaceSubscription, marketplaceSubscription);

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync(filter);
                return await cursor.FirstOrDefaultAsync();
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<PaginatedResponse> GetPaginatedResourcesAsync(int pageSize = 10, DateTime? timeStamp = null, bool showActiveOnly = true)
        {
            var builder = Builders<MarketplaceSaasResourceEntity>.Filter;
            var filter = builder.Empty;
            DateTime? lastTimeStamp = null;
            List<MarketplaceSaasResourceEntity> entities = new List<MarketplaceSaasResourceEntity>();
            IFindFluent<MarketplaceSaasResourceEntity, MarketplaceSaasResourceEntity> cursor = null;

            if (showActiveOnly)
            {
                filter = builder.Eq(u => u.Active, true);
            }

            await _rateLimiter.WaitAsync();
            try
            {
                if (timeStamp == null)
                {
                    cursor = _collection.Find(filter).Limit(pageSize);
                }
                else
                {
                    if (timeStamp.Value.Kind != DateTimeKind.Utc)
                    {
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} should be in UTC", timeStamp));
                    }

                    var paginationFilter = Builders<MarketplaceSaasResourceEntity>.Filter.Lt(u => u.CreatedUTC, timeStamp);
                    cursor = _collection.Find(paginationFilter).Limit(pageSize);
                }

                entities = await cursor.SortByDescending(item => item.CreatedUTC).ToListAsync();
                lastTimeStamp = GetLastTimeStamp(entities, pageSize);
                return new PaginatedResponse() { Entities = entities, LastTimeStamp = lastTimeStamp, PageSize = pageSize };
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<bool> SoftDeleteAsync(MarketplaceSubscription marketplaceSubscription)
        {
            var builder = Builders<MarketplaceSaasResourceEntity>.Filter;
            var filter = builder.Eq(u => u.MarketplaceSubscription, marketplaceSubscription);
            var update = Builders<MarketplaceSaasResourceEntity>.Update.Set(u => u.Active, false).Set(u => u.LastModifiedUTC, _timeSource.UtcNow);

            await _rateLimiter.WaitAsync();
            try
            {
                var updateResult = await _collection.UpdateOneAsync(filter, update);
                return updateResult.ModifiedCount == 1;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<bool> DeleteAsync(MarketplaceSubscription marketplaceSubscription)
        {
            var builder = Builders<MarketplaceSaasResourceEntity>.Filter;
            var filter = builder.Eq(u => u.MarketplaceSubscription, marketplaceSubscription);

            await _rateLimiter.WaitAsync();
            try
            {
                var deleteResult = await _collection.DeleteOneAsync(filter);
                return deleteResult.DeletedCount == 1;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task<MarketplaceSaasResourceEntity> UpdateAsync(MarketplaceSaasResourceEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await _rateLimiter.WaitAsync();
            try
            {
                entity.LastModifiedUTC = _timeSource.UtcNow;
                await _collection.ReplaceOneAsync(e => e.MarketplaceSubscription == entity.MarketplaceSubscription, entity);
                return entity;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        private DateTime? GetLastTimeStamp(List<MarketplaceSaasResourceEntity> entities, int pageSize)
        {
            DateTime? lastTimeStamp = null;

            if (!(entities.Count < pageSize))
            {
                lastTimeStamp = entities.Last().CreatedUTC;
            }

            return lastTimeStamp;
        }
    }
}
