//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Moq;
using Serilog;

namespace Microsoft.Liftr.Monitoring.Whale.Poseidon.Tests
{
    /// <summary>
    /// Provider for dedicated implementations of IMonitoringSvcEventHubEntityDataSource
    /// and IMonitoringSvcMonitoredEntityDataSource, used only by Poseidon.
    /// </summary>
    public static class PoseidonDatabaseProvider
    {
        /// <summary>
        /// Provider for a dedicated implementation of IEventHubEntityDataSource.
        /// </summary>
        public static IEventHubEntityDataSource GetEventHubDataSource()
        {
            var mongoOptions = ConfigurationLoader.GetMongoOptions();
            var loggerMock = new Mock<ILogger>();
            var factory = new MongoCollectionsFactory(mongoOptions, loggerMock.Object);

            var collection = factory.GetOrCreateEventHubEntityCollection(
                mongoOptions.EventHubSourceEntityCollectionName);

            return new EventHubEntityDataSource(collection, factory.MongoWaitQueueProtector, new SystemTimeSource());
        }

        /// <summary>
        /// Provider for a dedicated implementation of IMonitoringRelationshipDataSource.
        /// </summary>
        public static IMonitoringRelationshipDataSource<MonitoringRelationship> GetMonitoringRelationshipDataSource()
        {
            var mongoOptions = ConfigurationLoader.GetMongoOptions();
            var loggerMock = new Mock<ILogger>();
            var factory = new MongoCollectionsFactory(mongoOptions, loggerMock.Object);

            var collection = factory.GetOrCreateMonitoringCollection<MonitoringRelationship>(
                mongoOptions.MonitoringRelationshipCollectionName);

            return new MonitoringRelationshipDataSource(collection, factory.MongoWaitQueueProtector, null, new SystemTimeSource());
        }

        /// <summary>
        /// Provider for a dedicated implementation of IMonitoringStatusDataSource.
        /// </summary>
        public static IMonitoringStatusDataSource<MonitoringStatus> GetMonitoringStatusDataSource()
        {
            var mongoOptions = ConfigurationLoader.GetMongoOptions();
            var loggerMock = new Mock<ILogger>();
            var factory = new MongoCollectionsFactory(mongoOptions, loggerMock.Object);

            var collection = factory.GetOrCreateMonitoringCollection<MonitoringStatus>(
                mongoOptions.MonitoringStatusCollectionName);

            return new MonitoringStatusDataSource(collection, factory.MongoWaitQueueProtector, null, new SystemTimeSource());
        }

        /// <summary>
        /// Provider for a dedicated implementation of IPartnerResourceDataSource.
        /// </summary>
        public static IPartnerResourceDataSource<PartnerResourceEntity> GetPartnerDataSource()
        {
            var mongoOptions = ConfigurationLoader.GetMongoOptions();
            var loggerMock = new Mock<ILogger>();
            var factory = new MongoCollectionsFactory(mongoOptions, loggerMock.Object);

            var collection = factory.GetOrCreateEntityCollection<PartnerResourceEntity>(
                mongoOptions.PartnerResourceEntityCollectionName);

            return new PartnerResourceDataSource(collection, factory.MongoWaitQueueProtector, new SystemTimeSource());
        }
    }
}
