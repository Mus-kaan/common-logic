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
    public class MonitoringRelationshipDataSource : MonitoringBaseEntityDataSource<MonitoringRelationship>, IMonitoringRelationshipDataSource<MonitoringRelationship>
    {
        private readonly IMongoCollection<MonitoringRelationship> _collection;
        private readonly MongoWaitQueueRateLimiter _rateLimiter;
        private readonly ITimeSource _timeSource;

        public MonitoringRelationshipDataSource(IMongoCollection<MonitoringRelationship> collection, MongoWaitQueueRateLimiter rateLimiter, Serilog.ILogger logger, ITimeSource timeSource)
            : base(collection, rateLimiter, logger, timeSource)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        /// <inheritdoc/>
        public async Task<IMonitoringRelationship> AddAsync(IMonitoringRelationship entity)
        {
            using var dbOperation = _logger.StartTimedOperation("AddMonitoringRelationship");
            try
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
            catch (Exception ex)
            {
                dbOperation.FailOperation(ex.Message);
                throw;
            }
        }
    }
}
