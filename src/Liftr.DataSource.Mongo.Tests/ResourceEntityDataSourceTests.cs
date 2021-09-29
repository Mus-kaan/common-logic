//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.DataSource.Mongo.Tests
{
    public sealed class ResourceEntityDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MockResourceEntity> _collectionScope;

        public ResourceEntityDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<MockResourceEntity>((db, collectionName) =>
            {
                var collection = collectionFactory.GetOrCreateEntityCollection<MockResourceEntity>(collectionName);
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
            IResourceEntityDataSource<MockResourceEntity> s = new MockEntityDataSource(_collectionScope.Collection, rateLimiter, ts);

            var rid = "/subscriptions/b0a321d2-3073-44f0-b012-6e60db53ae22/resourceGroups/ngx-test-sbi0920-eus-rg/providers/Microsoft.Storage/storageAccounts/stngxtestsbi0920eus";

            var mockEntity = new MockResourceEntity() { ResourceId = rid, VNet = "VnetId123" };

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

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);

                var exist = await s.ExistAsync(mockEntity.EntityId);
                Assert.True(exist);

                exist = await s.ExistByResourceIdAsync(mockEntity.ResourceId);
                Assert.True(exist);
            }

            // Can retrieve only by resource id.
            {
                var retrieved = await s.ListAsync(entity1.ResourceId);

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.First().ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }

            // Check empty/null result
            {
                Assert.Null(await s.GetAsync(ObjectId.GenerateNewId().ToString()));

                Assert.Empty(await s.ListAsync(entity1.ResourceId + "asdasd"));
            }

            // Same EntityId throws.
            await Assert.ThrowsAsync<DuplicatedKeyException>(async () =>
            {
                var entity = new MockResourceEntity() { EntityId = entity1.EntityId, ResourceId = "asdasdas" };
                await s.AddAsync(entity);
            });

            // Same Resource Id is OK.
            {
                var entity = new MockResourceEntity() { ResourceId = rid, VNet = "VnetId456" };
                await s.AddAsync(entity);
            }

            var entities = await s.ListAsync(rid);
            Assert.Equal(2, entities.Count());

            // Same Resource Id with disabled.
            {
                var entity = new MockResourceEntity() { ResourceId = rid, VNet = "VnetId456", Active = false };
                await s.AddAsync(entity);
            }

            {
                var activeEntities = await s.ListAsync(rid);
                Assert.Equal(2, activeEntities.Count());
            }

            {
                var allEntities = await s.ListAsync(rid, showActiveOnly: false);
                Assert.Equal(3, allEntities.Count());
            }

            // can retrieve all available active entities
            {
                var allAvailableActive = await s.ListAsync();
                Assert.Equal(2, allAvailableActive.Count());
            }

            // can retrieve all available entities
            {
                var allAvailable = await s.ListAsync(showActiveOnly: false);
                Assert.Equal(3, allAvailable.Count());
            }

            // can update entity
            {
                ts.Add(TimeSpan.FromSeconds(1.53));
                var e1 = await s.GetAsync(mockEntity.EntityId);
                var e2 = await s.GetAsync(mockEntity.EntityId);

                e1.VNet = "newVnet11111";
                ts.Add(TimeSpan.FromSeconds(1.53));
                await s.UpdateAsync(e1);

                ts.Add(TimeSpan.FromSeconds(1.53));
                var retrieved = await s.GetAsync(mockEntity.EntityId);
                Assert.Equal("newVnet11111", retrieved.VNet);

                // ETag is different
                e2.VNet = "newVnet2222";
                ts.Add(TimeSpan.FromSeconds(1.53));
                await Assert.ThrowsAsync<UpdateConflictException>(async () =>
                {
                    await s.UpdateAsync(e2);
                });

                ts.Add(TimeSpan.FromSeconds(1.53));
                e1 = await s.GetAsync(mockEntity.EntityId);
                e1.VNet = "newVnet3333";

                ts.Add(TimeSpan.FromSeconds(1.53));
                await s.UpdateAsync(e1);

                ts.Add(TimeSpan.FromSeconds(1.53));
                retrieved = await s.GetAsync(mockEntity.EntityId);
                Assert.Equal("newVnet3333", retrieved.VNet);
            }
        }

        [CheckInValidation(skipLinux: true)]
        public async Task VerifyListAsync()
        {
            var ts = new MockTimeSource();
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            MockEntityDataSource s = new MockEntityDataSource(_collectionScope.Collection, rateLimiter, ts);

            var rid = "/subscriptions/b0a321d2-3073-44f0-b012-6e60db53ae22/resourceGroups/ngx-test-sbi0920-eus-rg/providers/Microsoft.Storage/storageAccounts/stngxtestsbi0920eus";
            int totalCount = 101;

            for (int i = 0; i < totalCount; i++)
            {
                var mockEntity = new MockResourceEntity() { ResourceId = rid + i, VNet = "VnetId123" + i };
                var entity1 = await s.AddAsync(mockEntity);
            }

            var directListResult = await s.ListAsync();
            Assert.Equal(totalCount, directListResult.Count());

            int asyncListCount = 0;
            var cursor = await s.ListWithCursorAsync();
            await cursor.ForEachAsync((record) =>
            {
                asyncListCount++;
            });

            Assert.Equal(totalCount, asyncListCount);
        }
    }
}
