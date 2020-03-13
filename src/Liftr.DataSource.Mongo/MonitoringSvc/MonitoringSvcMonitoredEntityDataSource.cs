//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringSvcMonitoredEntityDataSource : IMonitoringSvcMonitoredEntityDataSource
    {
        private readonly IMongoCollection<MonitoringSvcMonitoredEntity> _collection;

        public MonitoringSvcMonitoredEntityDataSource(IMongoCollection<MonitoringSvcMonitoredEntity> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        public async Task<IMonitoringSvcMonitoredEntity> AddEntityAsync(IMonitoringSvcMonitoredEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            try
            {
                entity.TimestampUTC = DateTime.UtcNow;
                await _collection.InsertOneAsync(entity as MonitoringSvcMonitoredEntity);
                return entity;
            }
            catch (Exception ex) when (ex.IsMongoDuplicatedKeyException())
            {
                throw new DuplicatedKeyException(ex);
            }
        }

        public async Task DeleteEntityAsync(string monitoredResourceId)
        {
            var filter = Builders<MonitoringSvcMonitoredEntity>.Filter.Eq(u => u.MonitoredResourceId, monitoredResourceId);
            await _collection.DeleteOneAsync(filter);
        }

        public async Task<IEnumerable<IMonitoringSvcMonitoredEntity>> GetAllEntityAsync()
        {
            var builder = Builders<MonitoringSvcMonitoredEntity>.Filter;
            var filter = builder.Eq(u => u.Enabled, true);
            var cursor = await _collection.FindAsync<MonitoringSvcMonitoredEntity>(filter);
            return cursor.ToEnumerable();
        }

        public async Task<IEnumerable<IMonitoringSvcMonitoredEntity>> GetAllEntityByMonitoringResourceIdAsync(string monitoringResourceId)
        {
            var builder = Builders<MonitoringSvcMonitoredEntity>.Filter;
            var filter = builder.Eq(u => u.MonitoringResourceId, monitoringResourceId) & builder.Eq(u => u.Enabled, true);
            var cursor = await _collection.FindAsync<MonitoringSvcMonitoredEntity>(filter);
            return cursor.ToEnumerable();
        }

        public async Task<IMonitoringSvcMonitoredEntity> GetEntityAsync(string monitoredResourceId)
        {
            var filter = Builders<MonitoringSvcMonitoredEntity>.Filter.Eq(u => u.Enabled, true);
            var cursor = await _collection.FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<IMonitoringSvcMonitoredEntity>> GetEntitiesAsync(string monitoredResourceId)
        {
            var filter = Builders<MonitoringSvcMonitoredEntity>.Filter.Eq(u => u.Enabled, true);
            filter &= Builders<MonitoringSvcMonitoredEntity>.Filter.Eq(u => u.MonitoredResourceId, monitoredResourceId);
            var cursor = await _collection.FindAsync(filter);
            return await cursor.ToListAsync();
        }

        public async Task<IEnumerable<string>> GetAllMonitoringRresourcesAsync()
        {
            var filter = Builders<MonitoringSvcMonitoredEntity>.Filter.Eq(u => u.Enabled, true);
            var cursor = await _collection
                .Find(filter)
                .Project(u => u.MonitoringResourceId)
                .ToListAsync();
            return cursor.Distinct();
        }
    }
}
