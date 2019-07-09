//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
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
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.CreateEntityCollectionAsync<MockResourceEntity>(collectionName).Result;
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
            IResourceEntityDataSource<MockResourceEntity> s = new MockEntityDataSource(_collectionScope.Collection);

            var subId1 = Guid.NewGuid().ToString();
            var subId2 = Guid.NewGuid().ToString();
            var rg1 = "resourceGroupName1";

            var entity1 = new MockResourceEntity() { SubscriptionId = subId1, ResourceGroup = rg1, Name = "entityName1", VNet = "VnetId123" };

            await s.AddEntityAsync(entity1);

            // Can retrieve.
            {
                var retrieved = await s.GetEntityAsync(entity1.SubscriptionId, entity1.ResourceGroup, entity1.Name);

                Assert.Equal(subId1, retrieved.SubscriptionId);
                Assert.Equal(rg1, retrieved.ResourceGroup);
                Assert.Equal("VnetId123", retrieved.VNet);

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }

            // Can retrieve only by name.
            {
                var retrieved = await s.GetEntityAsync(entity1.Name);

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }

            Assert.Null(await s.GetEntityAsync(entity1.SubscriptionId, entity1.ResourceGroup, entity1.Name + "asdasd"));

            Assert.Null(await s.GetEntityAsync(entity1.Name + "asdasd"));

            // Same EntityId throws.
            await Assert.ThrowsAsync<DuplicatedKeyException>(async () =>
            {
                var entity = new MockResourceEntity() { EntityId = entity1.EntityId, SubscriptionId = subId2, ResourceGroup = rg1, Name = "entityName2" };
                await s.AddEntityAsync(entity);
            });

            // Same Name throws.
            await Assert.ThrowsAsync<DuplicatedKeyException>(async () =>
            {
                var entity = new MockResourceEntity() { SubscriptionId = subId2, ResourceGroup = rg1, Name = entity1.Name };
                await s.AddEntityAsync(entity);
            });

            // Add multiple entities.
            for (int i = 0; i < 50; i++)
            {
                var entity = new MockResourceEntity();
                entity.SubscriptionId = i % 2 == 1 ? subId1 : subId2;
                entity.ResourceGroup = "resourceGroupName" + ((i % 3) + 1);
                entity.Name = "name" + i;
                await s.AddEntityAsync(entity);
            }

            Assert.Empty(await s.ListEntitiesAsync(subId1, rg1 + "asdasdasd"));

            int originaleCnt = 9;
            var enties = await s.ListEntitiesAsync(subId1, rg1);
            Assert.Equal(originaleCnt, enties.Count());

            for (int i = 0; i < originaleCnt; i++)
            {
                var e = enties[i];
                Assert.True(await s.DeleteEntityAsync(e.SubscriptionId, e.ResourceGroup, e.Name));
                Assert.Equal(originaleCnt - i - 1, (await s.ListEntitiesAsync(subId1, rg1)).Count());
            }

            Assert.False(await s.DeleteEntityAsync(entity1.SubscriptionId, entity1.ResourceGroup, entity1.Name));
        }
    }
}
