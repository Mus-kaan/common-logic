//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringStatusDataSource : MonitoringBaseEntityDataSource<MonitoringStatus>, IMonitoringStatusDataSource<MonitoringStatus>
    {
        private readonly IMongoCollection<MonitoringStatus> _collection;
        private readonly MongoWaitQueueRateLimiter _rateLimiter;
        private readonly ITimeSource _timeSource;

        public MonitoringStatusDataSource(
            IMongoCollection<MonitoringStatus> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
            : base(collection, rateLimiter, timeSource)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        /// <inheritdoc/>
        public async Task<IMonitoringStatus> AddOrUpdateAsync(IMonitoringStatus entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (string.IsNullOrEmpty(entity.TenantId))
            {
                throw new ArgumentException($"'{nameof(entity.TenantId)}' cannot be empty.");
            }

            if (!ResourceId.TryParse(entity.MonitoredResourceId, out _) &&
                !entity.MonitoredResourceId.OrdinalStartsWith("/SUBSCRIPTIONS/"))
            {
                throw new ArgumentException($"'{nameof(entity.MonitoredResourceId)}' '{entity.MonitoredResourceId}' is not in valid format.");
            }

            if (!ObjectId.TryParse(entity.PartnerEntityId, out _))
            {
                throw new ArgumentException($"'{nameof(entity.PartnerEntityId)}' is not in valid object id format.");
            }

            // Get existing entity, checking for possible update
            var existing = await GetAsync(entity.TenantId, entity.PartnerEntityId, entity.MonitoredResourceId);

            if (existing != null)
            {
                // Treat as update request
                return await UpdateAsync(
                    existing.TenantId, existing.PartnerEntityId, existing.MonitoredResourceId, entity.IsMonitored, entity.Reason);
            }
            else
            {
                // Treat as add request
                return await AddAsync(entity);
            }
        }

        /// <summary>
        /// Add a new monitoring status entity.
        /// </summary>
        private async Task<IMonitoringStatus> AddAsync(IMonitoringStatus entity)
        {
            var mappedEntity = new MonitoringStatus()
            {
                MonitoredResourceId = entity.MonitoredResourceId,
                PartnerEntityId = entity.PartnerEntityId,
                TenantId = entity.TenantId,
                IsMonitored = entity.IsMonitored,
                Reason = entity.Reason,
                LastModifiedAtUTC = _timeSource.UtcNow,
            };

            await _rateLimiter.WaitAsync();
            try
            {
                await _collection.InsertOneAsync(mappedEntity);
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

        /// <summary>
        /// Update the monitoring status and reason for a given entity.
        /// </summary>
        private async Task<IMonitoringStatus> UpdateAsync(string tenantId, string partnerObjectId, string monitoredResourceId, bool isMonitored, string reason)
        {
            var filter = Builders<MonitoringStatus>.Filter.Eq(u => u.TenantId, tenantId) &
                Builders<MonitoringStatus>.Filter.Eq(u => u.PartnerEntityId, partnerObjectId) &
                Builders<MonitoringStatus>.Filter.Eq(u => u.MonitoredResourceId, monitoredResourceId);

            var update = Builders<MonitoringStatus>.Update
                .Set(u => u.IsMonitored, isMonitored)
                .Set(u => u.Reason, reason)
                .Set(u => u.LastModifiedAtUTC, _timeSource.UtcNow);

            await _rateLimiter.WaitAsync();
            try
            {
                var updatedEntity = await _collection.FindOneAndUpdateAsync<MonitoringStatus>(filter, update);
                return updatedEntity;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
    }
}
