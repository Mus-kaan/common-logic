//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.DataSource.Mongo.Tests.MonitoringSvc
{
    public sealed class EventHubEntityDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<EventHubEntity> _collectionScope;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1004:Avoid calling System.Threading.Tasks.Task<TResult>.Result", Justification = "<Pending>")]
        public EventHubEntityDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<EventHubEntity>((db, collectionName) =>
            {
                var collection = collectionFactory.GetOrCreateEventHubEntityCollection(collectionName);
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [CheckInValidation(skipLinux: true)]
        public async Task BasicDataSourceUsageAsync()
        {
            var ts = new MockTimeSource();
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            var s = new EventHubEntityDataSource(_collectionScope.Collection, rateLimiter, ts);

            var location1 = "westus";
            var location2 = "eastus2";

            var rp = MonitoringResourceProvider.Datadog;

            var mockEntity = new EventHubEntity()
            {
                Name = "name1",
                Namespace = "ns1",
                AuthorizationRuleId = "mockAuthRuleId",
                EventHubConnectionString = "mockEHConnectionString",
                StorageConnectionString = "mockSAConnectionString",
                Location = location1,
                ResourceProvider = rp,
                CreatedAtUTC = DateTime.UtcNow,
            };

            await s.AddAsync(mockEntity);

            mockEntity.Name = "name2";
            mockEntity.Namespace = "ns2";
            await s.AddAsync(mockEntity);

            mockEntity.Name = "name3";
            mockEntity.Namespace = "ns3";
            await s.AddAsync(mockEntity);

            mockEntity.Location = location2;

            mockEntity.Name = "name4";
            mockEntity.Namespace = "ns4";
            await s.AddAsync(mockEntity);

            mockEntity.Name = "name5";
            mockEntity.Namespace = "ns5";
            await s.AddAsync(mockEntity);

            mockEntity.Name = "name6";
            mockEntity.Namespace = "ns6";
            mockEntity.ResourceProvider = MonitoringResourceProvider.Logz;
            await s.AddAsync(mockEntity);

            var list = await s.ListAsync(rp);
            Assert.Equal(5, list.Count());

            list = await s.ListAsync();
            Assert.Equal(6, list.Count());

            // Test update
            {
                var evh = list.First(i => i.Namespace.OrdinalEquals("ns6"));
                Assert.True(evh.Active);
                Assert.True(evh.IngestionEnabled);
                await s.UpdateAsync(evh.Namespace, ingestEnabled: false, active: false);

                list = await s.ListAsync();
                evh = list.First(i => i.Namespace.OrdinalEquals("ns6"));
                Assert.False(evh.Active);
                Assert.False(evh.IngestionEnabled);
            }

            list = await s.ListAsync(rp, location1);
            Assert.Equal(3, list.Count());

            list = await s.ListAsync(rp, location2);
            Assert.Equal(2, list.Count());

            var deleteCount = await s.DeleteAsync(rp);
            Assert.Equal(5, deleteCount);

            list = await s.ListAsync(rp);
            Assert.Empty(list);
        }
    }
}
