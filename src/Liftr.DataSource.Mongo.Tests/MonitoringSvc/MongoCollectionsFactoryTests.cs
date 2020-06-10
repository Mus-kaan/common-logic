//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Serilog.Core;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo.Tests.MonitoringSvc
{
    public sealed class MongoCollectionsFactoryTests
    {
        private readonly MongoCollectionsFactory _factory;
        private readonly MonitoringSvcMongoOptions _monitoringSvcMongoOptions;

        public MongoCollectionsFactoryTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            _monitoringSvcMongoOptions = new MonitoringSvcMongoOptions()
            {
                EventHubSourceEntityCollectionName = "mockEventHubSourceEntityCollection",
                MonitoringRelationshipCollectionName = "mockMonitoredEntityCollection",
                PartnerResourceEntityCollectionName = "mockPartnerResourceEntityCollection",
            };
            _factory = new MongoCollectionsFactory(option, Logger.None);
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task BasicOperationAsync()
        {
            await _factory.DeleteCollectionAsync(_monitoringSvcMongoOptions.EventHubSourceEntityCollectionName);
            await _factory.DeleteCollectionAsync(_monitoringSvcMongoOptions.MonitoringRelationshipCollectionName);
            await _factory.DeleteCollectionAsync(_monitoringSvcMongoOptions.PartnerResourceEntityCollectionName);

            await _factory.GetOrCreateEventHubEntityCollectionAsync(_monitoringSvcMongoOptions.EventHubSourceEntityCollectionName);
            await _factory.GetOrCreateMonitoringRelationshipCollectionAsync(_monitoringSvcMongoOptions.MonitoringRelationshipCollectionName);
            await _factory.GetOrCreatePartnerResourceEntityCollectionAsync(_monitoringSvcMongoOptions.PartnerResourceEntityCollectionName);

            await _factory.GetCollectionAsync<EventHubEntity>(_monitoringSvcMongoOptions.EventHubSourceEntityCollectionName);
            await _factory.DeleteCollectionAsync(_monitoringSvcMongoOptions.EventHubSourceEntityCollectionName);

            await _factory.GetCollectionAsync<MonitoringRelationship>(_monitoringSvcMongoOptions.MonitoringRelationshipCollectionName);
            await _factory.DeleteCollectionAsync(_monitoringSvcMongoOptions.MonitoringRelationshipCollectionName);

            await _factory.GetCollectionAsync<PartnerResourceEntity>(_monitoringSvcMongoOptions.PartnerResourceEntityCollectionName);
            await _factory.DeleteCollectionAsync(_monitoringSvcMongoOptions.PartnerResourceEntityCollectionName);
        }
    }
}
