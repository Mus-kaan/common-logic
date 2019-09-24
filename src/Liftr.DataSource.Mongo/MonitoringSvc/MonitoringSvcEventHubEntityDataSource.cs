//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringSvcEventHubEntityDataSource : IMonitoringSvcEventHubEntityDataSource
    {
        private readonly IMongoCollection<MonitoringSvcEventHubEntity> _collection;

        public MonitoringSvcEventHubEntityDataSource(IMongoCollection<MonitoringSvcEventHubEntity> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        public async Task<IMonitoringSvcEventHubEntity> GetEntityAsync(MonitoringSvcType partnerSvcType, string location)
        {
            var builder = Builders<MonitoringSvcEventHubEntity>.Filter;
            var filter = builder.Eq(u => u.PartnerServiceType, partnerSvcType) & builder.Eq(u => u.Location, location) & builder.Eq(u => u.Enabled, true);
            var cursor = await _collection.FindAsync<MonitoringSvcEventHubEntity>(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        public async Task<IMonitoringSvcEventHubEntity> GetEntityAsync(string monitoringSvcResourceProviderType, string location)
        {
            var builder = Builders<MonitoringSvcEventHubEntity>.Filter;
            var filter = builder.Eq(u => u.MonitoringSvcResourceProviderType, monitoringSvcResourceProviderType) & builder.Eq(u => u.Location, location) & builder.Eq(u => u.Enabled, true);
            var cursor = await _collection.FindAsync<MonitoringSvcEventHubEntity>(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<IMonitoringSvcEventHubEntity>> ListEntityAsync()
        {
            var builder = Builders<MonitoringSvcEventHubEntity>.Filter;
            var filter = builder.Eq(u => u.Enabled, true);
            var cursor = await _collection.FindAsync<MonitoringSvcEventHubEntity>(filter);
            return cursor.ToEnumerable();
        }
    }
}
