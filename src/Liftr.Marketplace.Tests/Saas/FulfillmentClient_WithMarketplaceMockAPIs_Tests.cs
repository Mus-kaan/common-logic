//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Models;
using Moq;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Marketplace.Saas.Tests
{
    /// <summary>
    /// This class validates the FulfillmentClient by making http calls against the actual marketplace mock apis
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#mock-apis
    /// </summary>
    public class FulfillmentClient_WithMarketplaceMockAPIs_Tests
    {
        private const string marketplaceSaasApiVersion = "2018-09-15";
        private readonly Uri _marketplaceSaasUri = new Uri("https://marketplaceapi.microsoft.com/");
        private readonly MarketplaceFulfillmentClient _fulfillmentClient;
        private readonly MarketplaceSubscription _marketplaceMockSubscription = new MarketplaceSubscription(Guid.Parse("37f9dea2-4345-438f-b0bd-03d40d28c7e0"));

        public FulfillmentClient_WithMarketplaceMockAPIs_Tests()
        {
            var logger = new Mock<ILogger>().Object;
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(_marketplaceSaasUri, marketplaceSaasApiVersion, logger, () => Task.FromResult("mockToken")), logger);
        }

        // TO DO: Should be skipped in official build as it needs internet
        [Fact]
        public async Task Can_resolve_subscription_Async()
        {
            var expectedSubscription = new ResolvedMarketplaceSubscription()
            {
                OfferId = "FabrikamDisasterRevovery",
                PlanId = "Gold",
                MarketplaceSubscription = _marketplaceMockSubscription,
                SubscriptionName = "Fabrikam solution for Joe's team",
            };

            var resolveToken = "testResolveToken";
            var resolvedSubscription = await _fulfillmentClient.ResolveSaaSSubscriptionAsync(resolveToken, CancellationToken.None);
            resolvedSubscription.Should().BeEquivalentTo(expectedSubscription);
        }

        [Fact]
        public async Task Can_activate_subscription_Async()
        {
            Func<Task> act = async () => { await _fulfillmentClient.ActivateSaaSSubscriptionAsync(new ActivateSubscriptionRequest(_marketplaceMockSubscription, "Gold", 100)); };
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Throws_exception_if_activation_doesnot_succeed_Async()
        {
            var marketplaceSubscription = new MarketplaceSubscription(Guid.NewGuid());

            await Assert.ThrowsAsync<MarketplaceException>(async () => await _fulfillmentClient.ActivateSaaSSubscriptionAsync(
                new ActivateSubscriptionRequest(marketplaceSubscription, "NOT_EXISTING_PLAN", 100), CancellationToken.None));
        }

        [Fact]
        public async Task Can_get_subscription_operation_Async()
        {
            var operationId = Guid.Parse("529f53e1-c04b-49c8-881c-c49fb5c6fada");

            var subscriptionOperation = await _fulfillmentClient.GetOperationAsync(_marketplaceMockSubscription, operationId);

            subscriptionOperation.MarketplaceSubscription.Should().BeEquivalentTo(_marketplaceMockSubscription);
            Assert.Equal(subscriptionOperation.Id, operationId);
        }

        [Fact]
        public async Task Can_get_pending_operations_Async()
        {
            var subscriptionOperation = await _fulfillmentClient.ListPendingOperationsAsync(_marketplaceMockSubscription);
            subscriptionOperation.Should().HaveCount(2);
        }

        [Fact]
        public async Task Can_update_operation_Async()
        {
            var operationId = Guid.Parse("529f53e1-c04b-49c8-881c-c49fb5c6fada");

            var operationUpdate = new OperationUpdate("Gold", 120, OperationUpdateStatus.Success);
            await _fulfillmentClient.UpdateOperationAsync(_marketplaceMockSubscription, operationId, operationUpdate);
        }
    }
}
