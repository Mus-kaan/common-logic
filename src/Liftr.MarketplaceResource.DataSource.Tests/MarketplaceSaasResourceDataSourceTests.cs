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
        private readonly MockTimeSource _ts = new MockTimeSource();

        public MarketplaceSaasResourceDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<MarketplaceSaasResourceEntity>((db, collectionName) =>
            {
                var collection = collectionFactory.GetOrCreateMarketplaceEntityCollection(collectionName);
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
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            var dataSource = new MarketplaceSaasResourceDataSource(_collectionScope.Collection, rateLimiter, _ts);

            var marketplaceSubscription1 = new MarketplaceSubscription(Guid.NewGuid());

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
        }

        [CheckInValidation(skipLinux: true)]
        public async Task PaginatedDataSourceResourcesAsync()
        {
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            var dataSource = new MarketplaceSaasResourceDataSource(_collectionScope.Collection, rateLimiter, _ts);

            var marketplaceSubscription2 = new MarketplaceSubscription(Guid.NewGuid());
            var marketplaceSubscription3 = new MarketplaceSubscription(Guid.NewGuid());
            var marketplaceSubscription4 = new MarketplaceSubscription(Guid.NewGuid());
            var marketplaceSubscription5 = new MarketplaceSubscription(Guid.NewGuid());

            var subscriptionDetails = new MarketplaceSubscriptionDetails()
            {
                Name = "test-name",
                PlanId = "planid",
                OfferId = "offerId",
                PublisherId = "publisherId",
                Beneficiary = new SaasBeneficiary() { TenantId = "tenantId" },
                Id = marketplaceSubscription2.ToString(),
                SaasSubscriptionStatus = SaasSubscriptionStatus.Subscribed,
            };

            var saasResource2 = new MarketplaceSaasResourceEntity(
                marketplaceSubscription2,
                MarketplaceSubscriptionDetailsEntity.From(subscriptionDetails),
                BillingTermTypes.Monthly);

            var saasResource3 = new MarketplaceSaasResourceEntity(
                marketplaceSubscription3,
                MarketplaceSubscriptionDetailsEntity.From(subscriptionDetails),
                BillingTermTypes.Monthly);

            var saasResource4 = new MarketplaceSaasResourceEntity(
                marketplaceSubscription4,
                MarketplaceSubscriptionDetailsEntity.From(subscriptionDetails),
                BillingTermTypes.Monthly);

            var saasResource5 = new MarketplaceSaasResourceEntity(
                marketplaceSubscription5,
                MarketplaceSubscriptionDetailsEntity.From(subscriptionDetails),
                BillingTermTypes.Monthly);
            {
                // Fetch resources while there are no records in the database
                var allResources = await dataSource.GetPaginatedResourcesAsync(3);
                allResources.Entities.Should().HaveCount(0);
                allResources.LastTimeStamp.Should().BeNull();
            }

            {
                // Lists all resources according to created at time
                var entity2 = await dataSource.AddAsync(saasResource2);
                var entity3 = await dataSource.AddAsync(saasResource3);
                var entity4 = await dataSource.AddAsync(saasResource4);
                var entity5 = await dataSource.AddAsync(saasResource5);

                var entity2Time = entity2.CreatedUTC;
                entity2.CreatedUTC = entity2Time.Add(new TimeSpan(3, 0, 0));

                var entity3Time = entity3.CreatedUTC;
                entity3.CreatedUTC = entity3Time.Add(new TimeSpan(5, 0, 0));

                var entity4Time = entity4.CreatedUTC;
                entity4.CreatedUTC = entity4Time.Add(new TimeSpan(7, 0, 0));

                var entity5Time = entity5.CreatedUTC;
                entity5.CreatedUTC = entity5Time.Add(new TimeSpan(9, 0, 0));

                var newEntity5 = await dataSource.UpdateAsync(entity5);
                var newEntity4 = await dataSource.UpdateAsync(entity4);
                var newEntity3 = await dataSource.UpdateAsync(entity3);
                var newEntity2 = await dataSource.UpdateAsync(entity2);

                var allResources = await dataSource.GetPaginatedResourcesAsync(3);

                allResources.Entities.Should().HaveCount(3);
                allResources.Entities.Should().Contain(resource => resource.ToJson(false).OrdinalEquals(saasResource4.ToJson(false)));
                allResources.Entities.Should().Contain(resource => resource.ToJson(false).OrdinalEquals(saasResource5.ToJson(false)));
                Assert.Equal(allResources.LastTimeStamp, newEntity3.CreatedUTC);

                // Fetch the next page of resources
                DateTime? timeStamp = allResources.LastTimeStamp;
                var nextResources = await dataSource.GetPaginatedResourcesAsync(2, timeStamp);
                nextResources.Entities.Should().Contain(resource => resource.ToJson(false).OrdinalEquals(saasResource2.ToJson(false)));
            }

            {
                // Continuation token is null when all resources are listed
                var allResources = await dataSource.GetPaginatedResourcesAsync(10);

                allResources.Entities.Should().HaveCount(4);
                allResources.LastTimeStamp.Should().BeNull();
            }
        }
    }
}
