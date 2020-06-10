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
        private readonly ITimeSource _timeSource;

        public EventHubEntityDataSource(IMongoCollection<EventHubEntity> collection, ITimeSource timeSource)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        public async Task AddAsync(IEventHubEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (string.IsNullOrEmpty(entity.ResourceProvider))
            {
                throw new ArithmeticException(nameof(entity.ResourceProvider));
            }

            var mappedEntity = new EventHubEntity()
            {
                ResourceProvider = entity.ResourceProvider.ToUpperInvariant(),
                Namespace = entity.Namespace,
                Name = entity.Name,
                Location = entity.Location,
                EventHubConnectionString = entity.EventHubConnectionString,
                StorageConnectionString = entity.StorageConnectionString,
                AuthorizationRuleId = entity.AuthorizationRuleId,
                CreatedAtUTC = _timeSource.UtcNow,
            };

            await _collection.InsertOneAsync(mappedEntity);
        }

        public async Task<IEnumerable<IEventHubEntity>> ListAsync(string resourceProvider)
        {
            if (string.IsNullOrEmpty(resourceProvider))
            {
                throw new ArgumentNullException(nameof(resourceProvider));
            }

            resourceProvider = resourceProvider.ToUpperInvariant();
            var filter = Builders<EventHubEntity>.Filter.Eq(i => i.ResourceProvider, resourceProvider);
            var cursor = await _collection.FindAsync<EventHubEntity>(filter);
            return await cursor.ToListAsync();
        }

        public async Task<IEnumerable<IEventHubEntity>> ListAsync(string resourceProvider, string location)
        {
            if (string.IsNullOrEmpty(resourceProvider))
            {
                throw new ArgumentNullException(nameof(resourceProvider));
            }

            resourceProvider = resourceProvider.ToUpperInvariant();
            var filter = Builders<EventHubEntity>.Filter.Eq(i => i.ResourceProvider, resourceProvider) &
                Builders<EventHubEntity>.Filter.Eq(i => i.Location, location);
            var cursor = await _collection.FindAsync<EventHubEntity>(filter);
            return await cursor.ToListAsync();
        }

        public async Task<int> DeleteAsync(string resourceProvider)
        {
            if (string.IsNullOrEmpty(resourceProvider))
            {
                throw new ArgumentNullException(nameof(resourceProvider));
            }

            resourceProvider = resourceProvider.ToUpperInvariant();
            var filter = Builders<EventHubEntity>.Filter.Eq(i => i.ResourceProvider, resourceProvider);
            var deleteResult = await _collection.DeleteManyAsync(filter);
            return (int)deleteResult.DeletedCount;
        }
    }
}
