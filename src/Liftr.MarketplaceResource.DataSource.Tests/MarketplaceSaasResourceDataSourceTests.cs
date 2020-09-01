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
    public sealed class MarketplaceSaasResourceDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MarketplaceSaasResourceEntity> _collectionScope;

        public MarketplaceSaasResourceDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<MarketplaceSaasResourceEntity>((db, collectionName) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.GetOrCreateMarketplaceEntityCollectionAsync(collectionName).Result;
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
            var dataSource = new MarketplaceSaasResourceDataSource(_collectionScope.Collection, rateLimiter, ts);

            var marketplaceSubscription1 = new MarketplaceSubscription(Guid.NewGuid());
            var marketplaceSubscription2 = new MarketplaceSubscription(Guid.NewGuid());

            var subscriptionDetails = new MarketplaceSubscriptionDetails()
            {
                Name = "test-name",
                PlanId = "planid",
                OfferId = "offerId",
                PublisherId = "publisherId",
                Beneficiary = new SaasBeneficiary() { TenantId = "tenantId" },
                Id = marketplaceSubscription1.ToString(),
                SaasSubscriptionStatus = SaasSubscriptionStatus.Subscribed,
            };

            var saasResource1 = new MarketplaceSaasResourceEntity(
                marketplaceSubscription1,
                MarketplaceSubscriptionDetailsEntity.From(subscriptionDetails),
                BillingTermTypes.Monthly);

            var saasResource2 = new MarketplaceSaasResourceEntity(
                marketplaceSubscription2,
                MarketplaceSubscriptionDetailsEntity.From(subscriptionDetails),
                BillingTermTypes.Monthly);

            var entity1 = await dataSource.AddAsync(saasResource1);

            // Can retrieve.
            {
                var retrieved = await dataSource.GetAsync(marketplaceSubscription1);

                Assert.Equal(marketplaceSubscription1, retrieved.MarketplaceSubscription);
                retrieved.SubscriptionDetails.ToJson().Should().Be(subscriptionDetails.ToJson());

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }

            {
                // Can list
                var entity2 = dataSource.AddAsync(saasResource2);

                var allResources = await dataSource.GetAllResourcesAsync();

                allResources.Should().HaveCount(2);
                allResources.Should().Contain(resource => resource.ToJson(false).OrdinalEquals(saasResource1.ToJson(false)));
                allResources.Should().Contain(resource => resource.ToJson(false).OrdinalEquals(saasResource2.ToJson(false)));
            }
        }
    }
}
