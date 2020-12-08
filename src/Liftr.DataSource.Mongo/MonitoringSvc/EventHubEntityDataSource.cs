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
    public class EventHubEntityDataSource : IEventHubEntityDataSource
    {
        private readonly IMongoCollection<EventHubEntity> _collection;
        private readonly MongoWaitQueueRateLimiter _rateLimiter;
        private readonly ITimeSource _timeSource;

        public EventHubEntityDataSource(IMongoCollection<EventHubEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        public async Task AddAsync(IEventHubEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var mappedEntity = new EventHubEntity()
            {
                ResourceProvider = entity.ResourceProvider,
                Namespace = entity.Namespace,
                Name = entity.Name,
                Location = entity.Location,
                EventHubConnectionString = entity.EventHubConnectionString,
                StorageConnectionString = entity.StorageConnectionString,
                AuthorizationRuleId = entity.AuthorizationRuleId,
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

        public async Task<IEnumerable<IEventHubEntity>> ListAsync()
        {
            var filter = Builders<EventHubEntity>.Filter.Empty;

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync<EventHubEntity>(filter);
                return await cursor.ToListAsync();
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task<IEventHubEntity> UpdateAsync(string eventhubNamespaceName, bool ingestEnabled, bool active)
        {
            var filter = Builders<EventHubEntity>.Filter.Eq(i => i.Namespace, eventhubNamespaceName);
            var update = Builders<EventHubEntity>.Update
                .Set(e => e.IngestionEnabled, ingestEnabled)
                .Set(e => e.Active, active);

            await _rateLimiter.WaitAsync();
            try
            {
                await _collection.FindOneAndUpdateAsync<EventHubEntity>(filter, update);
                var cursor = await _collection.FindAsync<EventHubEntity>(filter);
                return await cursor.FirstOrDefaultAsync();
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task<IEnumerable<IEventHubEntity>> ListAsync(MonitoringResourceProvider resourceProvider)
        {
            var filter = Builders<EventHubEntity>.Filter.Eq(i => i.ResourceProvider, resourceProvider);

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync<EventHubEntity>(filter);
                return await cursor.ToListAsync();
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task<IEnumerable<IEventHubEntity>> ListAsync(MonitoringResourceProvider resourceProvider, string location)
        {
            var filter = Builders<EventHubEntity>.Filter.Eq(i => i.ResourceProvider, resourceProvider) &
                Builders<EventHubEntity>.Filter.Eq(i => i.Location, location);

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync<EventHubEntity>(filter);
                return await cursor.ToListAsync();
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task<int> DeleteAsync(MonitoringResourceProvider resourceProvider)
        {
            var filter = Builders<EventHubEntity>.Filter.Eq(i => i.ResourceProvider, resourceProvider);

            await _rateLimiter.WaitAsync();
            try
            {
                var deleteResult = await _collection.DeleteManyAsync(filter);
                return (int)deleteResult.DeletedCount;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task<bool> DeleteAsync(string documentId)
        {
            var filter = Builders<EventHubEntity>.Filter.Eq(i => i.DocumentObjectId, documentId);

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
    }
}
