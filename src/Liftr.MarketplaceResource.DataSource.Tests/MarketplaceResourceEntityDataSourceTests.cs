//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.MarketplaceResource.DataSource.Models;
using Microsoft.Liftr;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Liftr.MarketplaceResource.DataSource.Tests
{
    public sealed class MarketplaceResourceEntityDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MarketplaceResourceEntity> _collectionScope;

        public MarketplaceResourceEntityDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<MarketplaceResourceEntity>((db, collectionName) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.GetOrCreateEntityCollectionAsync<MarketplaceResourceEntity>(collectionName).Result;
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
#pragma warning restore CS0618 // Type or member is obsolete
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [SkipInOfficialBuild]
        public async Task BasicDataSourceUsageAsync()
        {
            var ts = new MockTimeSource();
            var dataSource = new MarketplaceResourceEntityDataSource(_collectionScope.Collection, ts);

            var rid = "/subscriptions/b0a321d2-3073-44f0-b012-6e60db53ae22/resourceGroups/ngx-test-sbi0920-eus-rg/providers/Microsoft.Storage/storageAccounts/stngxtestsbi0920eus";
            var marketplaceSubscription = new MarketplaceSubscription(Guid.NewGuid());
            var tenantId = "testTenantId";
            var saasResourceId = "providers/Microsoft.SaaS/saasresources/1e0b1ed8-1e35-ab0b-0c39-60aacd8982d9";

            var marketplaceResourceEntity = new MarketplaceResourceEntity(marketplaceSubscription, saasResourceId, rid, tenantId);
            var entity1 = await dataSource.AddEntityAsync(marketplaceResourceEntity);

            // Can retrieve.
            {
                var retrieved = await dataSource.GetEntityAsync(entity1.EntityId);

                Assert.Equal(rid, retrieved.ResourceId);
                Assert.Equal(marketplaceSubscription.Id, retrieved.MarketplaceSubscription.Id);

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }

            // Can retrieve by marketplace subscription id.
            {
                var retrieved = await dataSource.GetEntityForMarketplaceSubscriptionAsync(marketplaceSubscription);
                Assert.Equal(rid, retrieved.ResourceId);
                Assert.Equal(marketplaceSubscription.Id, retrieved.MarketplaceSubscription.Id);
                Assert.Equal(tenantId, retrieved.TenantId);
            }
        }
    }
}
