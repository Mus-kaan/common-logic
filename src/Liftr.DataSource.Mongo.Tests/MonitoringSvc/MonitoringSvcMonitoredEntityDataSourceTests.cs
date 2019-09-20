//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.DataSource.Mongo.Tests.MonitoringSvc
{
    public sealed class MonitoringSvcMonitoredEntityDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MonitoringSvcMonitoredEntity> _collectionScope;

        public MonitoringSvcMonitoredEntityDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<MonitoringSvcMonitoredEntity>((db, collectionName) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.CreateCollection<MonitoringSvcMonitoredEntity>(collectionName);
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
#pragma warning restore CS0618 // Type or member is obsolete
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [Fact]
        public async Task BasicDataSourceUsageAsync()
        {
            var ts = new MockTimeSource();
            var s = new MonitoringSvcMonitoredEntityDataSource(_collectionScope.Collection);

            var monitoredResourceId = "mockMonitoredResourceId";
            var monitoringResourceId = "mockMonitoringResourceId";
            var partnerCredential = "mockPartnerCredential";
            var partnerServiceType = Contracts.MonitoringSvc.MonitoringSvcType.DataDog;
            var resourceType = "mockResourceType";
            uint priority = 1;

            var mockEntity = new MonitoringSvcMonitoredEntity()
            {
                MonitoredResourceId = monitoredResourceId,
                MonitoringResourceId = monitoringResourceId,
                PartnerCredential = partnerCredential,
                PartnerServiceType = partnerServiceType,
                ResourceType = resourceType,
                Priority = priority,
                Enabled = true,
            };

            // Can add
            await s.AddEntityAsync(mockEntity);

            // Can retrieve.
            {
                var retrieved = await s.GetEntityAsync(mockEntity.MonitoredResourceId);

                Assert.Equal(monitoredResourceId, retrieved.MonitoredResourceId);
                Assert.Equal(monitoringResourceId, retrieved.MonitoringResourceId);
                Assert.Equal(partnerCredential, retrieved.PartnerCredential);
                Assert.Equal(partnerServiceType, retrieved.PartnerServiceType);
                Assert.Equal(resourceType, retrieved.ResourceType);
                Assert.Equal(priority, retrieved.Priority);
            }

            // List entity
            var list = await s.GetAllEntityByMonitoringResourceIdAsync(monitoringResourceId);
            Assert.True(list.Count() == 1);

            // Delete entity
            await s.DeleteEntityAsync(monitoredResourceId);
            list = await s.GetAllEntityByMonitoringResourceIdAsync(monitoringResourceId);
            Assert.True(!list.Any());
        }
    }
}
