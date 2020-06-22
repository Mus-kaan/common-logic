//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Flurl.Http.Testing;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Models;
using Microsoft.Liftr.Marketplace.Tests;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Marketplace.Saas.Tests
{
    /// <summary>
    /// This class validates the FulfillmentClient by mocking the http calls
    /// </summary>
    public class FulfillmentClient_WithMockHttpHandler_Tests
    {
        private const string marketplaceEndpoint = "https://test.com";
        private const string marketplaceSaasApiVersion = "2020-02-03";
        private readonly MarketplaceFulfillmentClient _fulfillmentClient;

        public FulfillmentClient_WithMockHttpHandler_Tests()
        {
            var logger = new Mock<ILogger>().Object;
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(new Uri(marketplaceEndpoint), marketplaceSaasApiVersion, logger, () => Task.FromResult("mockToken")), logger);
        }

        [Fact]
        public async Task Can_resolve_subscription_Async()
        {
            var cancellationToken = CancellationToken.None;
            using var httpTest = new HttpTest();
            var expectedSubscription = new ResolvedMarketplaceSubscription()
            {
                OfferId = "FabrikamDisasterRevovery",
                PlanId = "Gold",
                MarketplaceSubscription = new MarketplaceSubscription(Guid.NewGuid()),
                SubscriptionName = "Fabrikam solution for Joe's team",
            };

            httpTest.RespondWithJson(expectedSubscription);

            var resolveToken = "testResolveToken";
            var resolvedSubscription = await _fulfillmentClient.ResolveSaaSSubscriptionAsync(resolveToken, cancellationToken);
            resolvedSubscription.Should().BeEquivalentTo(expectedSubscription);
        }

        [Fact]
        public async Task Throws_if_subscription_not_resolved_Async()
        {
            var cancellationToken = CancellationToken.None;
            using var httpTest = new HttpTest();
            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            httpTest.ResponseQueue.Enqueue(httpResponseMessage);

            var resolveToken = "testResolveToken";
            await Assert.ThrowsAsync<MarketplaceException>(async () => await _fulfillmentClient.ResolveSaaSSubscriptionAsync(resolveToken, cancellationToken));
        }

        [Fact]
        public async Task Calls_the_resolve_api_with_token_header_Async()
        {
            var cancellationToken = CancellationToken.None;
            using var httpTest = new HttpTest();
            var expectedSubscription = new ResolvedMarketplaceSubscription()
            {
                OfferId = "FabrikamDisasterRevovery",
                PlanId = "Gold",
                MarketplaceSubscription = new MarketplaceSubscription(Guid.NewGuid()),
                SubscriptionName = "Fabrikam solution for Joe's team",
            };

            httpTest.RespondWithJson(expectedSubscription);
            var resolveToken = "testResolveToken";
            var resolvedSubscription = await _fulfillmentClient.ResolveSaaSSubscriptionAsync(resolveToken, cancellationToken);

            httpTest.ShouldHaveCalled($"{marketplaceEndpoint}/api/saas/subscriptions/resolve")
                    .WithVerb(HttpMethod.Post)
                    .WithContentType("application/json")
                    .WithHeader("x-ms-marketplace-token", resolveToken)
                    .Times(1);
        }

        [Fact]
        public async Task Can_activate_subscription_Async()
        {
            var cancellationToken = CancellationToken.None;
            using var httpTest = new HttpTest();
            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
            httpTest.ResponseQueue.Enqueue(httpResponseMessage);
            var subscription = new MarketplaceSubscription(Guid.Parse("37f9dea2-4345-438f-b0bd-03d40d28c7e0"));

            Func<Task> act = async () => { await _fulfillmentClient.ActivateSaaSSubscriptionAsync(new ActivateSubscriptionRequest(subscription, "Gold", 100), cancellationToken); };

            await act.Should().NotThrowAsync();
            httpTest.ShouldHaveCalled($"{marketplaceEndpoint}/api/saas/subscriptions/{subscription}/activate")
                    .WithVerb(HttpMethod.Post)
                    .Times(1);
        }

        [Fact]
        public async Task Throws_if_activation_doesnot_succeed_Async()
        {
            var cancellationToken = CancellationToken.None;
            using var httpTest = new HttpTest();
            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            httpTest.ResponseQueue.Enqueue(httpResponseMessage);

            var subscription = new MarketplaceSubscription(Guid.Parse("37f9dea2-4345-438f-b0bd-03d40d28c7e0"));
            await Assert.ThrowsAsync<MarketplaceException>(async () => await _fulfillmentClient.ActivateSaaSSubscriptionAsync(new ActivateSubscriptionRequest(subscription, "Gold", 100), cancellationToken));
        }

        [Fact]
        public async Task Can_get_subscription_operation_Async()
        {
            var operationId = Guid.NewGuid();
            var subscription = new MarketplaceSubscription(Guid.NewGuid());

            var expectedOperation = new SubscriptionOperation()
            {
                Id = operationId,
                MarketplaceSubscription = subscription,
                OfferId = "testOfferId",
                PlanId = "plan",
                PublisherId = "publisher",
            };

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(expectedOperation);

            await _fulfillmentClient.GetOperationAsync(subscription, operationId);

            httpTest.ShouldHaveCalled($"{marketplaceEndpoint}/api/saas/subscriptions/{subscription.Id}/operations/{operationId}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
        }

        [Fact]
        public async Task Can_get_pending_operations_Async()
        {
            var subscription = new MarketplaceSubscription(Guid.NewGuid());
            var operationId = Guid.NewGuid();

            var expectedOperation = new SubscriptionOperation()
            {
                Id = operationId,
                MarketplaceSubscription = subscription,
                OfferId = "testOfferId",
                PlanId = "plan",
                PublisherId = "publisher",
            };
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new List<SubscriptionOperation>() { expectedOperation });

            var subscriptionOperation = await _fulfillmentClient.ListPendingOperationsAsync(subscription);

            subscriptionOperation.Should().HaveCount(1);
            httpTest.ShouldHaveCalled($"{marketplaceEndpoint}/api/saas/subscriptions/{subscription.Id}/operations")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
        }

        [Fact]
        public async Task Can_update_operation_Async()
        {
            var operationId = Guid.NewGuid();
            var subscription = new MarketplaceSubscription(Guid.NewGuid());

            var operationUpdate = new OperationUpdate("plan", 0, OperationUpdateStatus.Success);

            using var httpTest = new HttpTest();
            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
            httpTest.ResponseQueue.Enqueue(httpResponseMessage);

            await _fulfillmentClient.UpdateOperationAsync(subscription, operationId, operationUpdate);

            httpTest.ShouldHaveCalled($"{marketplaceEndpoint}/api/saas/subscriptions/{subscription.Id}/operations/{operationId}")
                    .WithVerb(HttpMethod.Patch)
                    .WithRequestBody(operationUpdate.ToJson())
                    .Times(1);
        }

        [Fact]
        public async Task Throws_exception_if_update_doesnot_succeed_Async()
        {
            var operationId = Guid.NewGuid();
            var subscription = new MarketplaceSubscription(Guid.NewGuid());

            var operationUpdate = new OperationUpdate("BAD_PLAN", 0, OperationUpdateStatus.Success);

            using var httpTest = new HttpTest();
            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            httpTest.ResponseQueue.Enqueue(httpResponseMessage);

            await Assert.ThrowsAsync<MarketplaceException>(async () => await _fulfillmentClient.UpdateOperationAsync(subscription, operationId, operationUpdate));
        }

        [Fact]
        public async Task Can_delete_subscription_Async()
        {
            var subscription = new MarketplaceSubscription(Guid.NewGuid());

            var operationLocation = "https://mockoperationlocation.com";
            using var httpTest = new HttpTest();
            using var response1 = MockAsyncOperationHelper.AcceptedResponseWithOperationLocation(operationLocation);
            using var response2 = MockAsyncOperationHelper.SuccessResponseWithSucceededStatus(new SubscriptionOperation());
            httpTest.ResponseQueue.Enqueue(response1);
            httpTest.ResponseQueue.Enqueue(response2);

            await _fulfillmentClient.DeleteSubscriptionAsync(subscription);

            httpTest.ShouldHaveCalled(operationLocation)
                .WithVerb(HttpMethod.Get)
                .Times(1);

            httpTest.ShouldHaveCalled($"{marketplaceEndpoint}/api/saas/subscriptions/{subscription}")
                .WithVerb(HttpMethod.Delete)
                .Times(1);
        }
    }
}
