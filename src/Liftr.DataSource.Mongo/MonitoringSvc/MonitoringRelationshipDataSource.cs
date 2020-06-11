//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringRelationshipDataSource : IMonitoringRelationshipDataSource
    {
        private readonly IMongoCollection<MonitoringRelationship> _collection;
        private readonly ITimeSource _timeSource;

        public MonitoringRelationshipDataSource(IMongoCollection<MonitoringRelationship> collection, ITimeSource timeSource)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        /// <inheritdoc/>
        public async Task<IMonitoringRelationship> AddAsync(IMonitoringRelationship entity)
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

            // TODO: make the check and insert atomic. This is a workaround for now.
            // Idealy it should be fixed by the unique compound key of <PartnerObjectId>, <MonitoredResourceId>.
            // However, this is not working since the db is shared.
            var existing = await GetAsync(entity.TenantId, entity.PartnerEntityId, entity.MonitoredResourceId);
            if (existing != null)
            {
                throw new DuplicatedKeyException($"Duplicating {nameof(entity.TenantId)} '{entity.TenantId}', {nameof(entity.PartnerEntityId)} '{entity.PartnerEntityId}', {nameof(entity.MonitoredResourceId)} '{entity.MonitoredResourceId}'");
            }

            var mappedEntity = new MonitoringRelationship()
            {
                MonitoredResourceId = entity.MonitoredResourceId,
                PartnerEntityId = entity.PartnerEntityId,
                TenantId = entity.TenantId,
                AuthorizationRuleId = entity.AuthorizationRuleId,
                EventhubName = entity.EventhubName,
                DiagnosticSettingsName = entity.DiagnosticSettingsName,
                CreatedAtUTC = _timeSource.UtcNow,
            };

            try
            {
                await _collection.InsertOneAsync(mappedEntity);
                return entity;
            }
            catch (Exception ex) when (ex.IsMongoDuplicatedKeyException())
            {
                throw new DuplicatedKeyException(ex);
            }
        }

        /// <inheritdoc/>
        public async Task<IMonitoringRelationship> GetAsync(string tenantId, string partnerObjectId, string monitoredResourceId)
        {
            var filter = Builders<MonitoringRelationship>.Filter.Eq(u => u.TenantId, tenantId) &
                Builders<MonitoringRelationship>.Filter.Eq(u => u.PartnerEntityId, partnerObjectId) &
                Builders<MonitoringRelationship>.Filter.Eq(u => u.MonitoredResourceId, monitoredResourceId);
            var cursor = await _collection.FindAsync<MonitoringRelationship>(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<IMonitoringRelationship>> ListByMonitoredResourceAsync(string tenantId, string monitoredResourceId)
        {
            var filter = Builders<MonitoringRelationship>.Filter.Eq(u => u.TenantId, tenantId) &
                Builders<MonitoringRelationship>.Filter.Eq(u => u.MonitoredResourceId, monitoredResourceId);
            var cursor = await _collection.FindAsync<MonitoringRelationship>(filter);
            return await cursor.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<IMonitoringRelationship>> ListByPartnerResourceAsync(string tenantId, string partnerResourceObjectId)
        {
            var filter = Builders<MonitoringRelationship>.Filter.Eq(u => u.TenantId, tenantId) &
                Builders<MonitoringRelationship>.Filter.Eq(u => u.PartnerEntityId, partnerResourceObjectId);
            var cursor = await _collection.FindAsync<MonitoringRelationship>(filter);
            return await cursor.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<int> DeleteAsync(string tenantId, string partnerResourceObjectId = null, string monitoredResourceId = null)
        {
            if (string.IsNullOrEmpty(partnerResourceObjectId) && string.IsNullOrEmpty(monitoredResourceId))
            {
                throw new ArgumentNullException(nameof(partnerResourceObjectId), $"Both '{nameof(partnerResourceObjectId)}' and '{nameof(monitoredResourceId)}' cannot be null at the same time.");
            }

            var filter = Builders<MonitoringRelationship>.Filter.Eq(u => u.TenantId, tenantId);

            if (!string.IsNullOrEmpty(partnerResourceObjectId))
            {
                filter = filter & Builders<MonitoringRelationship>.Filter.Eq(u => u.PartnerEntityId, partnerResourceObjectId);
            }

            if (!string.IsNullOrEmpty(monitoredResourceId))
            {
                filter = filter & Builders<MonitoringRelationship>.Filter.Eq(u => u.MonitoredResourceId, monitoredResourceId);
            }

            var deleteResult = await _collection.DeleteManyAsync(filter);
            return (int)deleteResult.DeletedCount;
        }
    }
}
