//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class StorageEntityDataSource : IStorageEntityDataSource
    {
        private readonly IMongoCollection<StorageEntity> _collection;
        private readonly MongoWaitQueueRateLimiter _rateLimiter;
        private readonly ITimeSource _timeSource;

        public StorageEntityDataSource(IMongoCollection<StorageEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        public async Task AddAsync(IStorageEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var mappedEntity = new StorageEntity()
            {
                AccountName = entity.AccountName,
                ResourceId = entity.ResourceId,
                LogForwarderRegion = entity.LogForwarderRegion.NormalizedAzRegion(),
                StorageRegion = entity.StorageRegion.NormalizedAzRegion(),
                Priority = entity.Priority,
                IngestionEnabled = entity.IngestionEnabled,
                Active = entity.Active,
                CreatedAtUTC = _timeSource.UtcNow,
            };

            await _rateLimiter.WaitAsync();
            try
            {
                await _collection.InsertOneAsync(mappedEntity);
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task<bool> DeleteAsync(string documentId)
        {
            var filter = Builders<StorageEntity>.Filter.Eq(i => i.DocumentObjectId, documentId);

            await _rateLimiter.WaitAsync();
            try
            {
                var deleteResult = await _collection.DeleteOneAsync(filter);
                return (int)deleteResult.DeletedCount == 1;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task<IEnumerable<IStorageEntity>> ListAsync(StoragePriority priority, string logForwarderRegion = null)
        {
            var filter = Builders<StorageEntity>.Filter.Eq(u => u.Priority, priority)
                & Builders<StorageEntity>.Filter.Eq(u => u.IngestionEnabled, true);

            if (!string.IsNullOrEmpty(logForwarderRegion))
            {
                filter = filter & Builders<StorageEntity>.Filter.Eq(u => u.LogForwarderRegion, logForwarderRegion.NormalizedAzRegion());
            }

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync<StorageEntity>(filter);
                return await cursor.ToListAsync();
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
    }
}
