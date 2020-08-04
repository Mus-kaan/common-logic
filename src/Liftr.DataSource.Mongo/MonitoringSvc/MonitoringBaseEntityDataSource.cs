//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringBaseEntityDataSource<TMonitoringEntity>
        : IMonitoringBaseEntityDataSource<TMonitoringEntity> where TMonitoringEntity : MonitoringBaseEntity
    {
        private readonly IMongoCollection<TMonitoringEntity> _collection;
        private readonly MongoWaitQueueRateLimiter _rateLimiter;
        private readonly ITimeSource _timeSource;

        public MonitoringBaseEntityDataSource(IMongoCollection<TMonitoringEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        /// <inheritdoc/>
        public async Task<TMonitoringEntity> GetAsync(string tenantId, string partnerObjectId, string monitoredResourceId)
        {
            var filter = Builders<TMonitoringEntity>.Filter.Eq(u => u.TenantId, tenantId) &
                Builders<TMonitoringEntity>.Filter.Eq(u => u.PartnerEntityId, partnerObjectId) &
                Builders<TMonitoringEntity>.Filter.Eq(u => u.MonitoredResourceId, monitoredResourceId);

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync<TMonitoringEntity>(filter);
                return await cursor.FirstOrDefaultAsync();
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TMonitoringEntity>> ListByMonitoredResourceAsync(string tenantId, string monitoredResourceId)
        {
            var filter = Builders<TMonitoringEntity>.Filter.Eq(u => u.TenantId, tenantId) &
                Builders<TMonitoringEntity>.Filter.Eq(u => u.MonitoredResourceId, monitoredResourceId);

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync<TMonitoringEntity>(filter);
                return await cursor.ToListAsync();
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TMonitoringEntity>> ListByPartnerResourceAsync(string tenantId, string partnerObjectId)
        {
            var filter = Builders<TMonitoringEntity>.Filter.Eq(u => u.TenantId, tenantId) &
                Builders<TMonitoringEntity>.Filter.Eq(u => u.PartnerEntityId, partnerObjectId);

            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync<TMonitoringEntity>(filter);
                return await cursor.ToListAsync();
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<int> DeleteAsync(string tenantId, string partnerObjectId = null, string monitoredResourceId = null)
        {
            if (string.IsNullOrEmpty(partnerObjectId) && string.IsNullOrEmpty(monitoredResourceId))
            {
                throw new ArgumentNullException(nameof(partnerObjectId), $"Both '{nameof(partnerObjectId)}' and '{nameof(monitoredResourceId)}' cannot be null at the same time.");
            }

            var filter = Builders<TMonitoringEntity>.Filter.Eq(u => u.TenantId, tenantId);

            if (!string.IsNullOrEmpty(partnerObjectId))
            {
                filter = filter & Builders<TMonitoringEntity>.Filter.Eq(u => u.PartnerEntityId, partnerObjectId);
            }

            if (!string.IsNullOrEmpty(monitoredResourceId))
            {
                filter = filter & Builders<TMonitoringEntity>.Filter.Eq(u => u.MonitoredResourceId, monitoredResourceId);
            }

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
    }
}
