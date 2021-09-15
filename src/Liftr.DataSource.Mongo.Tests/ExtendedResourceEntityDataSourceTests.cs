//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.DataSource.Mongo.Tests
{
    public sealed class ExtendedResourceEntityDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MockResourceEntity> _collectionScope;
        private readonly TestCollectionScope<ExtendedMockResourceEntity> _extendedCollectionScope;

        public ExtendedResourceEntityDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);

            var testDB = TestDBConnection.TestDatabase;
            var testCollectionName = TestDBConnection.RandomCollectionName();

            _collectionScope = new TestCollectionScope<MockResourceEntity>(testDB, testCollectionName, (db, collectionName) =>
             {
                 var collection = collectionFactory.GetOrCreateEntityCollection<MockResourceEntity>(collectionName);
                 return collection;
             });

            _extendedCollectionScope = new TestCollectionScope<ExtendedMockResourceEntity>(testDB, testCollectionName, (db, collectionName) =>
            {
                var collection = collectionFactory.GetOrCreateEntityCollection<ExtendedMockResourceEntity>(collectionName);
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
            _extendedCollectionScope.Dispose();
        }

        [CheckInValidation(skipLinux: true)]
        public async Task ExtraPropertyWillStillWorkAsync()
        {
            var ts = new MockTimeSource();
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            IResourceEntityDataSource<ExtendedMockResourceEntity> s = new MockExtendedEntityDataSource(_extendedCollectionScope.Collection, rateLimiter, ts);

            var rid = "/subscriptions/b0a321d2-3073-44f0-b012-6e60db53ae22/resourceGroups/ngx-test-sbi0920-eus-rg/providers/Microsoft.Storage/storageAccounts/stngxtestsbi0920eus";

            var mockEntity = new ExtendedMockResourceEntity() { ResourceId = rid, VNet = "VnetId123" };

            // Not exist before insert.
            {
                var exist = await s.ExistAsync(mockEntity.EntityId);
                Assert.False(exist);

                exist = await s.ExistByResourceIdAsync(mockEntity.ResourceId);
                Assert.False(exist);
            }

            var entity1 = await s.AddAsync(mockEntity);

            // Can retrieve.
            {
                var retrieved = await s.GetAsync(entity1.EntityId);

                Assert.Equal(rid.ToUpperInvariant(), retrieved.ResourceId);
                Assert.Equal("VnetId123", retrieved.VNet);
                Assert.Equal(Microsoft.Liftr.Contracts.ProvisioningState.Succeeded, retrieved.ProvisioningState);

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);

                var exist = await s.ExistAsync(mockEntity.EntityId);
                Assert.True(exist);

                exist = await s.ExistByResourceIdAsync(mockEntity.ResourceId);
                Assert.True(exist);
            }

            // Can retrieve for less properties.
            IResourceEntityDataSource<MockResourceEntity> ds = new MockEntityDataSource(_collectionScope.Collection, rateLimiter, ts);
            {
                var retrieved = await ds.GetAsync(entity1.EntityId);

                Assert.Equal(rid.ToUpperInvariant(), retrieved.ResourceId);
                Assert.Equal("VnetId123", retrieved.VNet);
            }
        }
    }
}
