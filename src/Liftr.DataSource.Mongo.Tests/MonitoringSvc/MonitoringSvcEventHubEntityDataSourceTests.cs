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

            var mockEntity = new MonitoringSvcEventHubEntity()
            {
                Name = "mockName",
                Namespace = "mockNameSpace",
                AuthorizationRuleId = "mockAuthRuleId",
                EventHubConnStr = "mockEHConnectionString",
                StorageConnStr = "mockSAConnectionString",
                Enabled = true,
                Location = "mockLocation",
                PartnerServiceType = MonitoringSvcType.DataDog,
                DataType = MonitoringSvcDataType.Log,
                MonitoringSvcResourceProviderType = "Microsoft.Datadog/datadogs",
                IsDataEncrypted = false,
            };

            await s.InsertEntityAsync(mockEntity);

            // Assert insertion successful
            var list = await s.ListEntityAsync();
            Assert.True(list.Count() == 1);

            // Can retrieve with partnerSvcType.
            var retrieved = await s.GetEntityAsync(mockEntity.PartnerServiceType, mockEntity.Location);
            AssertEqual(retrieved, mockEntity);

            // Can retrieve with resourceProviderType.
            retrieved = await s.GetEntityAsync(mockEntity.MonitoringSvcResourceProviderType, mockEntity.Location);
            AssertEqual(retrieved, mockEntity);

            // Can retrieve with resourceProviderType.
            retrieved = await s.GetEntityAsync(mockEntity.MonitoringSvcResourceProviderType, mockEntity.Location);
            AssertEqual(retrieved, mockEntity);

            // Can delete with eventHubNamespace
            await s.DeleteEntitiesAsync(mockEntity.Namespace);
            list = await s.ListEntityAsync();
            Assert.Empty(list);
        }

        private void AssertEqual(IMonitoringSvcEventHubEntity actual, MonitoringSvcEventHubEntity expected)
        {
            Assert.Equal(actual.Name, expected.Name);
            Assert.Equal(actual.Namespace, expected.Namespace);
            Assert.Equal(actual.AuthorizationRuleId, expected.AuthorizationRuleId);
            Assert.Equal(actual.EventHubConnStr, expected.EventHubConnStr);
            Assert.Equal(actual.StorageConnStr, expected.StorageConnStr);
            Assert.Equal(actual.Location, expected.Location);
            Assert.Equal(actual.DataType, expected.DataType);
            Assert.Equal(actual.PartnerServiceType, expected.PartnerServiceType);
            Assert.Equal(actual.MonitoringSvcResourceProviderType, expected.MonitoringSvcResourceProviderType);
            Assert.Equal(actual.IsDataEncrypted, expected.IsDataEncrypted);
        }
    }
}
