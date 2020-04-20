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
    public sealed class MonitoringSvcEventHubEntityDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MonitoringSvcEventHubEntity> _collectionScope;

        public MonitoringSvcEventHubEntityDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<MonitoringSvcEventHubEntity>((db, collectionName) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.CreateCollection<MonitoringSvcEventHubEntity>(collectionName);
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
#pragma warning restore CS0618 // Type or member is obsolete
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task BasicDataSourceUsageAsync()
        {
            var ts = new MockTimeSource();
            var s = new MonitoringSvcEventHubEntityDataSource(_collectionScope.Collection);

            var name = "mockName";
            var nameSpace = "mockNameSpace";
            var authruleid = "mockAuthRuleId";
            var eventHubConnectionString = "mockEHConnectionString";
            var storageConnectionString = "mockSAConnectionString";
            var location = "mockLocation";
            var resourceProviderType = "Microsoft.Datadog/datadogs";

            var mockEntity = new MonitoringSvcEventHubEntity()
            {
                Name = name,
                Namespace = nameSpace,
                AuthorizationRuleId = authruleid,
                EventHubConnStr = eventHubConnectionString,
                StorageConnStr = storageConnectionString,
                Enabled = true,
                Location = location,
                PartnerServiceType = MonitoringSvcType.DataDog,
                DataType = MonitoringSvcDataType.Log,
                MonitoringSvcResourceProviderType = resourceProviderType,
            };

            await _collectionScope.Collection.InsertOneAsync(mockEntity);

            // Can retrieve with partnerSvcType.
            {
                var retrieved = await s.GetEntityAsync(mockEntity.PartnerServiceType, mockEntity.Location);

                Assert.Equal(name, retrieved.Name);
                Assert.Equal(nameSpace, retrieved.Namespace);
                Assert.Equal(authruleid, retrieved.AuthorizationRuleId);
                Assert.Equal(eventHubConnectionString, retrieved.EventHubConnStr);
                Assert.Equal(storageConnectionString, retrieved.StorageConnStr);
                Assert.Equal(location, retrieved.Location);
                Assert.Equal(MonitoringSvcDataType.Log, retrieved.DataType);
                Assert.Equal(MonitoringSvcType.DataDog, retrieved.PartnerServiceType);
                Assert.Equal(resourceProviderType, retrieved.MonitoringSvcResourceProviderType);
            }

            // Can retrieve with resourceProviderType.
            {
                var retrieved = await s.GetEntityAsync(resourceProviderType, mockEntity.Location);

                Assert.Equal(name, retrieved.Name);
                Assert.Equal(nameSpace, retrieved.Namespace);
                Assert.Equal(authruleid, retrieved.AuthorizationRuleId);
                Assert.Equal(eventHubConnectionString, retrieved.EventHubConnStr);
                Assert.Equal(storageConnectionString, retrieved.StorageConnStr);
                Assert.Equal(location, retrieved.Location);
                Assert.Equal(MonitoringSvcDataType.Log, retrieved.DataType);
                Assert.Equal(MonitoringSvcType.DataDog, retrieved.PartnerServiceType);
                Assert.Equal(resourceProviderType, retrieved.MonitoringSvcResourceProviderType);
            }

            // List entity
            var list = await s.ListEntityAsync();
            Assert.True(list.Count() == 1);
        }
    }
}
