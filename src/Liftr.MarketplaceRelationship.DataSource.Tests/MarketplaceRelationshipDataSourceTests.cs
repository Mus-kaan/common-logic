//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.MarketplaceRelationship.DataSource;
using MongoDB.Bson;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Liftr.MarketplaceRelationship.DataSource.Test
{
    public sealed class MarketplaceRelationshipDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MarketplaceRelationshipEntity> _collectionScope;
        private readonly MockTimeSource _ts = new MockTimeSource();

        public MarketplaceRelationshipDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new GlobalMongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<MarketplaceRelationshipEntity>((db, collectionName) =>
            {
                var collection = collectionFactory.GetOrCreateMarketplaceRelationshipEntityCollection(collectionName);
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [CheckInValidation(skipLinux: true)]
        public async Task AddRetrieveRelationshipEntityAsync()
        {
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            var dataSource = new MarketplaceRelationshipEntityDataSource<MarketplaceRelationshipEntity>(_collectionScope.Collection, rateLimiter, _ts);

            var marketplaceSubscription = new MarketplaceSubscription(Guid.NewGuid());
            var resourceId1 = Guid.NewGuid().ToString();
            var tenantId1 = Guid.NewGuid().ToString();
            var region1 = "eastus";

            var entity = new MarketplaceRelationshipEntity(ObjectId.GenerateNewId().ToString(), resourceId1, region1, tenantId1, marketplaceSubscription);

            var entity1 = await dataSource.AddAsync(entity);

            // Can retrieve single entity.
            {
                var retrieved = await dataSource.GetAsync(entity1.EntityId);

                Assert.Equal(marketplaceSubscription, retrieved.MarketplaceSubscription);
                Assert.Equal(resourceId1.ToUpperInvariant(), retrieved.ResourceId);
                Assert.Equal(region1, retrieved.Region);
                Assert.Equal(tenantId1, retrieved.TenantId);

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }
        }

        [CheckInValidation(skipLinux: true)]
        public async Task RetrieveMultipleEntitiesAsync()
        {
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            var dataSource = new MarketplaceRelationshipEntityDataSource<MarketplaceRelationshipEntity>(_collectionScope.Collection, rateLimiter, _ts);

            var marketplaceSubscription = new MarketplaceSubscription(Guid.NewGuid());

            var resourceId1 = Guid.NewGuid().ToString();
            var tenantId1 = Guid.NewGuid().ToString();
            var region1 = "eastus";

            var resourceId2 = Guid.NewGuid().ToString();
            var tenantId2 = Guid.NewGuid().ToString();
            var region2 = "westus";

            var entity = new MarketplaceRelationshipEntity(ObjectId.GenerateNewId().ToString(), resourceId1, region1, tenantId1, marketplaceSubscription);
            var entity2 = new MarketplaceRelationshipEntity(ObjectId.GenerateNewId().ToString(), resourceId2, region2, tenantId2, marketplaceSubscription);

            var entity1 = await dataSource.AddAsync(entity);
            var entity3 = await dataSource.AddAsync(entity2);

            var expected = new[] { entity1, entity3 };

            // Can retrieve multiple entities.
            {
                var retrieved = await dataSource.ListAsync(marketplaceSubscription);

                Assert.Equal(expected.Count(), retrieved.Count());
                Assert.Equal(resourceId1.ToUpperInvariant(), retrieved.First().ResourceId);
                Assert.Equal(region1, retrieved.First().Region);
                Assert.Equal(tenantId1, retrieved.First().TenantId);

                var exceptedStr = expected.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }
        }
    }
}
