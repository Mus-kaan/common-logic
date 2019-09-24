//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringSvcVMExtensionDetailsEntityDataSource : IMonitoringSvcVMExtensionDetailsEntityDataSource
    {
        private readonly IMongoCollection<MonitoringSvcVMExtensionDetailsEntity> _collection;

        public MonitoringSvcVMExtensionDetailsEntityDataSource(IMongoCollection<MonitoringSvcVMExtensionDetailsEntity> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        public async Task<IMonitoringSvcVMExtensionDetailsEntity> GetEntityAsync(string resourceProviderType, string operatingSystem)
        {
            var builder = Builders<MonitoringSvcVMExtensionDetailsEntity>.Filter;
            var filter = builder.Eq(u => u.MonitoringSvcResourceProviderType, resourceProviderType) &
                builder.Eq(u => u.OperatingSystem, operatingSystem);
            var cursor = await _collection.FindAsync<MonitoringSvcVMExtensionDetailsEntity>(filter);
            return await cursor.FirstOrDefaultAsync();
        }
    }
}
