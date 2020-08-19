//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.MarketplaceResource.DataSource.Tests
{
    public sealed class MarketplaceResourceEntityDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MarketplaceResourceContainerEntity> _collectionScope;

        public MarketplaceResourceEntityDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<MarketplaceResourceContainerEntity>((db, collectionName) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.GetOrCreateEntityCollectionAsync<MarketplaceResourceContainerEntity>(collectionName).Result;
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
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            var dataSource = new MarketplaceResourceContainerEntityDataSource(_collectionScope.Collection, rateLimiter, ts);

            var rid = "/subscriptions/b0a321d2-3073-44f0-b012-6e60db53ae22/resourceGroups/ngx-test-sbi0920-eus-rg/providers/Microsoft.Storage/storageAccounts/stngxtestsbi0920eus";
            var marketplaceSubscription = new MarketplaceSubscription(Guid.NewGuid());
            var tenantId = "testTenantId";
            var saasResource = new MarketplaceSaasResourceEntity(
                marketplaceSubscription,
                new MarketplaceSubscriptionDetails()
                {
                    Name = "test-name",
                    PlanId = "planid",
                    OfferId = "offerId",
                    PublisherId = "publisherId",
                    Beneficiary = new SaasBeneficiary() { TenantId = "tenantId" },
                    Id = marketplaceSubscription.ToString(),
                },
                BillingTermTypes.Monthly);

            var marketplaceResourceEntity = new MarketplaceResourceContainerEntity(saasResource, rid, tenantId);
            var entity1 = await dataSource.AddAsync(marketplaceResourceEntity);

            // Can retrieve.
            {
                var retrieved = await dataSource.GetAsync(entity1.EntityId);

                Assert.Equal(rid.ToUpperInvariant(), retrieved.ResourceId);
                Assert.Equal(marketplaceSubscription.Id, retrieved.MarketplaceSaasResource.MarketplaceSubscription.Id);

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }

            // Can retrieve by marketplace subscription id.
            {
                var retrieved = await dataSource.GetEntityForMarketplaceSubscriptionAsync(marketplaceSubscription);
                Assert.Equal(rid.ToUpperInvariant(), retrieved.ResourceId);
                Assert.Equal(marketplaceSubscription, retrieved.MarketplaceSaasResource.MarketplaceSubscription);
                retrieved.MarketplaceSaasResource.SubscriptionDetails.Should().BeEquivalentTo(saasResource.SubscriptionDetails);
                Assert.Equal(tenantId, retrieved.TenantId);
            }
        }
    }
}
