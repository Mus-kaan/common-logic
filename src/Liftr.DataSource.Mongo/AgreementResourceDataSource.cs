//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class AgreementResourceDataSource : IAgreementResourceDataSource
    {
        private readonly IMongoCollection<AgreementResourceEntity> _collection;
        private readonly MongoWaitQueueRateLimiter _rateLimiter;
        private readonly ITimeSource _timeSource;

        public AgreementResourceDataSource(IMongoCollection<AgreementResourceEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
        {
            _collection = collection ?? throw new System.ArgumentNullException(nameof(collection));
            _rateLimiter = rateLimiter ?? throw new System.ArgumentNullException(nameof(rateLimiter));
            _timeSource = timeSource ?? throw new System.ArgumentNullException(nameof(timeSource));
        }

        public async Task AcceptAsync(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            // Get existing entity, checking for possible update
            var existing = await GetAsync(subscriptionId);

            if (existing)
            {
                await UpdateAsync(subscriptionId);
            }
            else
            {
                await AddAsync(subscriptionId);
            }
        }

        public async Task<bool> GetAsync(string subscriptionId)
        {
            var builder = Builders<AgreementResourceEntity>.Filter;
            var filter = builder.Eq(u => u.SubscriptionId, subscriptionId);

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync(filter);
                var entity = await cursor.FirstOrDefaultAsync();

                return entity != null;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        private async Task<AgreementResourceEntity> AddAsync(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            var entity = new AgreementResourceEntity(subscriptionId);
            entity.CreatedUTC = _timeSource.UtcNow;
            entity.LastModifiedUTC = _timeSource.UtcNow;
            entity.AcceptedUTC = _timeSource.UtcNow;

            await _rateLimiter.WaitAsync();
            try
            {
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

        private async Task<AgreementResourceEntity> UpdateAsync(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            var filter = Builders<AgreementResourceEntity>.Filter.Eq(e => e.SubscriptionId, subscriptionId);
            var update = Builders<AgreementResourceEntity>.Update
                .Set(e => e.AcceptedUTC, _timeSource.UtcNow)
                .Set(e => e.LastModifiedUTC, _timeSource.UtcNow);

            await _rateLimiter.WaitAsync();
            try
            {
                var updatedEntity = await _collection.FindOneAndUpdateAsync<AgreementResourceEntity>(filter, update);
                return updatedEntity;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
    }
}
