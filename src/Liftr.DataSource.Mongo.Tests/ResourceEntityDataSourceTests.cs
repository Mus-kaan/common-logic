//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using MongoDB.Bson;
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
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.GetOrCreateEntityCollectionAsync<MockResourceEntity>(collectionName).Result;
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
            IResourceEntityDataSource<MockResourceEntity> s = new MockEntityDataSource(_collectionScope.Collection, ts);

            var rid = "/subscriptions/b0a321d2-3073-44f0-b012-6e60db53ae22/resourceGroups/ngx-test-sbi0920-eus-rg/providers/Microsoft.Storage/storageAccounts/stngxtestsbi0920eus";

            var mockEntity = new MockResourceEntity() { ResourceId = rid, VNet = "VnetId123" };
            var entity1 = await s.AddEntityAsync(mockEntity);

            // Can retrieve.
            {
                var retrieved = await s.GetEntityAsync(entity1.EntityId);

                Assert.Equal(rid, retrieved.ResourceId);
                Assert.Equal("VnetId123", retrieved.VNet);

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }

            // Can retrieve only by resoure id.
            {
                var retrieved = await s.ListEntitiesByResourceIdAsync(entity1.ResourceId);

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.First().ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }

            Assert.Null(await s.GetEntityAsync(ObjectId.GenerateNewId().ToString()));

            Assert.Empty(await s.ListEntitiesByResourceIdAsync(entity1.ResourceId + "asdasd"));

            // Same EntityId throws.
            await Assert.ThrowsAsync<DuplicatedKeyException>(async () =>
            {
                var entity = new MockResourceEntity() { EntityId = entity1.EntityId, ResourceId = "asdasdas" };
                await s.AddEntityAsync(entity);
            });

            // Same Resource Id is OK.
            {
                var entity = new MockResourceEntity() { ResourceId = rid, VNet = "VnetId456" };
                await s.AddEntityAsync(entity);
            }

            var entities = await s.ListEntitiesByResourceIdAsync(rid);
            Assert.Equal(2, entities.Count());

            // Same Resource Id with disabled.
            {
                var entity = new MockResourceEntity() { ResourceId = rid, VNet = "VnetId456", Active = false };
                await s.AddEntityAsync(entity);
            }

            {
                var activeEntities = await s.ListEntitiesByResourceIdAsync(rid);
                Assert.Equal(2, activeEntities.Count());
            }

            {
                var allEntities = await s.ListEntitiesByResourceIdAsync(rid, showActiveOnly: false);
                Assert.Equal(3, allEntities.Count());
            }
        }
    }
}
