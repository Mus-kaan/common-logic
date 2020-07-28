//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Models;
using Microsoft.Liftr.Marketplace.Tests;
using Moq;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
        private static MarketplaceSubscription s_marketplaceSubscription = new MarketplaceSubscription(Guid.NewGuid());
        private static Guid s_operationId = Guid.NewGuid();
        private ILogger _logger;
        private MarketplaceFulfillmentClient _fulfillmentClient;

        public FulfillmentClient_WithMockHttpHandler_Tests()
        {
            _logger = new Mock<ILogger>().Object;
        }

        [Fact]
        public async Task Can_resolve_subscription_Async()
        {
            var cancellationToken = CancellationToken.None;
            var expectedSubscription = GetResolvedSubscription();

            using var handler = new MockHttpMessageHandler();
            using var httpClient = new HttpClient(handler, false);
            var resolveToken = "testResolveToken";
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(new Uri(marketplaceEndpoint), marketplaceSaasApiVersion, _logger, httpClient, () => Task.FromResult("mockToken")), _logger);

            var resolvedSubscription = await _fulfillmentClient.ResolveSaaSSubscriptionAsync(resolveToken, cancellationToken);
            resolvedSubscription.Should().BeEquivalentTo(expectedSubscription);
        }

        [Fact]
        public async Task Throws_if_subscription_not_resolved_Async()
        {
            var cancellationToken = CancellationToken.None;
            using var handler = new MockHttpMessageHandler(true);
            using var httpClient = new HttpClient(handler, false);
            var resolveToken = "testResolveToken";
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(new Uri(marketplaceEndpoint), marketplaceSaasApiVersion, _logger, httpClient, () => Task.FromResult("mockToken")), _logger);

            await Assert.ThrowsAsync<MarketplaceException>(async () => await _fulfillmentClient.ResolveSaaSSubscriptionAsync(resolveToken, cancellationToken));
        }

        [Fact]
        public async Task Can_activate_subscription_Async()
        {
            var cancellationToken = CancellationToken.None;
            using var handler = new MockHttpMessageHandler();
            using var httpClient = new HttpClient(handler, false);
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(new Uri(marketplaceEndpoint), marketplaceSaasApiVersion, _logger, httpClient, () => Task.FromResult("mockToken")), _logger);

            Func<Task> act = async () => { await _fulfillmentClient.ActivateSaaSSubscriptionAsync(new ActivateSubscriptionRequest(s_marketplaceSubscription, "Gold", 100), cancellationToken); };

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Throws_if_activation_doesnot_succeed_Async()
        {
            var cancellationToken = CancellationToken.None;
            using var handler = new MockHttpMessageHandler(true);
            using var httpClient = new HttpClient(handler, false);
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(new Uri(marketplaceEndpoint), marketplaceSaasApiVersion, _logger, httpClient, () => Task.FromResult("mockToken")), _logger);

            await Assert.ThrowsAsync<MarketplaceException>(async () => await _fulfillmentClient.ActivateSaaSSubscriptionAsync(new ActivateSubscriptionRequest(s_marketplaceSubscription, "Gold", 100), cancellationToken));
        }

        [Fact]
        public async Task Can_get_subscription_operation_Async()
        {
            var expectedOperation = GetSubscriptionOperation();

            using var handler = new MockHttpMessageHandler();
            using var httpClient = new HttpClient(handler, false);
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(new Uri(marketplaceEndpoint), marketplaceSaasApiVersion, _logger, httpClient, () => Task.FromResult("mockToken")), _logger);

            var actualOperation = await _fulfillmentClient.GetOperationAsync(s_marketplaceSubscription, s_operationId);

            Assert.Equal(expectedOperation.Id, actualOperation.Id);
        }

        [Fact]
        public async Task Can_get_pending_operations_Async()
        {
            var expectedOperation = GetSubscriptionOperation();

            using var handler = new MockHttpMessageHandler();
            using var httpClient = new HttpClient(handler, false);
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(new Uri(marketplaceEndpoint), marketplaceSaasApiVersion, _logger, httpClient, () => Task.FromResult("mockToken")), _logger);

            var subscriptionOperationList = await _fulfillmentClient.ListPendingOperationsAsync(s_marketplaceSubscription);
            var actualOperation = subscriptionOperationList.FirstOrDefault();

            Assert.Equal(expectedOperation.Id, actualOperation.Id);
        }

        [Fact]
        public async Task Can_update_operation_Async()
        {
            using var handler = new MockHttpMessageHandler();
            using var httpClient = new HttpClient(handler, false);
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(new Uri(marketplaceEndpoint), marketplaceSaasApiVersion, _logger, httpClient, () => Task.FromResult("mockToken")), _logger);

            var operationUpdate = new OperationUpdate("plan", 0, OperationUpdateStatus.Success);

            await _fulfillmentClient.UpdateOperationAsync(s_marketplaceSubscription, s_operationId, operationUpdate);
        }

        [Fact]
        public async Task Throws_exception_if_update_doesnot_succeed_Async()
        {
            using var handler = new MockHttpMessageHandler(true);
            using var httpClient = new HttpClient(handler, false);
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(new Uri(marketplaceEndpoint), marketplaceSaasApiVersion, _logger, httpClient, () => Task.FromResult("mockToken")), _logger);

            var operationUpdate = new OperationUpdate("BAD_PLAN", 0, OperationUpdateStatus.Success);

            await Assert.ThrowsAsync<MarketplaceException>(async () => await _fulfillmentClient.UpdateOperationAsync(s_marketplaceSubscription, s_operationId, operationUpdate));
        }

        [Fact]
        public async Task Can_delete_subscription_Async()
        {
            var subscription = new MarketplaceSubscription(Guid.NewGuid());
            using var handler = new MockHttpMessageHandler();
            using var httpClient = new HttpClient(handler, false);
            _fulfillmentClient = new MarketplaceFulfillmentClient(new MarketplaceRestClient(new Uri(marketplaceEndpoint), marketplaceSaasApiVersion, _logger, httpClient, () => Task.FromResult("mockToken")), _logger);

            Func<Task> act = async () => { await _fulfillmentClient.DeleteSubscriptionAsync(subscription); };
            await act.Should().NotThrowAsync();
        }

        internal static ResolvedMarketplaceSubscription GetResolvedSubscription()
        {
            return new ResolvedMarketplaceSubscription()
            {
                OfferId = "FabrikamDisasterRevovery",
                PlanId = "Gold",
                MarketplaceSubscription = s_marketplaceSubscription,
                SubscriptionName = "Fabrikam solution for Joe's team",
            };
        }

        internal static SubscriptionOperation GetSubscriptionOperation()
        {
            return new SubscriptionOperation()
            {
                Id = s_operationId,
                MarketplaceSubscription = s_marketplaceSubscription,
                OfferId = "testOfferId",
                PlanId = "plan",
                PublisherId = "publisher",
            };
        }

        internal class MockHttpMessageHandler : HttpMessageHandler
        {
            private bool _failure;

            public MockHttpMessageHandler(bool failure = false)
            {
                _failure = failure;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Yield();
                var response = new HttpResponseMessage();

                if (request.RequestUri.ToString().OrdinalContains("resolve") && request.Method == HttpMethod.Post && request.Headers.Contains("x-ms-marketplace-token") && !_failure)
                {
                    var resolvedSubscription = GetResolvedSubscription();
                    var resolvedSubscriptionResponse = JsonConvert.SerializeObject(resolvedSubscription);
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content = new StringContent(resolvedSubscriptionResponse, Encoding.UTF8, "application/json");
                }
                else if (request.RequestUri.ToString().OrdinalContains("resolve") && request.Method == HttpMethod.Post && request.Headers.Contains("x-ms-marketplace-token") && _failure)
                {
                    response.RequestMessage = new HttpRequestMessage();
                    response.RequestMessage.RequestUri = request.RequestUri;
                    response.StatusCode = HttpStatusCode.BadRequest;
                }
                else if (request.RequestUri.ToString().OrdinalContains("activate") && request.Method == HttpMethod.Post && !_failure)
                {
                    var activateResponse = JsonConvert.SerializeObject("activated");
                    response.Content = new StringContent(activateResponse, Encoding.UTF8, "application/json");
                    response.StatusCode = HttpStatusCode.OK;
                }
                else if (request.RequestUri.ToString().OrdinalContains("activate") && request.Method == HttpMethod.Post && _failure)
                {
                    response.RequestMessage = new HttpRequestMessage();
                    response.RequestMessage.RequestUri = request.RequestUri;
                    response.StatusCode = HttpStatusCode.BadRequest;
                }
                else if (request.RequestUri.ToString().OrdinalContains("operations/") && request.Method == HttpMethod.Get && !_failure)
                {
                    var subscriptionOperation = GetSubscriptionOperation();
                    var subscriptionOperationResponse = JsonConvert.SerializeObject(subscriptionOperation);
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content = new StringContent(subscriptionOperationResponse, Encoding.UTF8, "application/json");
                }
                else if (request.RequestUri.ToString().OrdinalContains("operations") && request.Method == HttpMethod.Get && !_failure)
                {
                    var subscriptionOperation = GetSubscriptionOperation();
                    var subscriptionOperationList = new List<SubscriptionOperation>() { subscriptionOperation };
                    var subscriptionOperationListResponse = JsonConvert.SerializeObject(subscriptionOperationList);
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content = new StringContent(subscriptionOperationListResponse, Encoding.UTF8, "application/json");
                }
                else if (request.RequestUri.ToString().OrdinalContains("operations/") && request.Method == HttpMethod.Patch && !_failure)
                {
                    var activateResponse = JsonConvert.SerializeObject("updated");
                    response.Content = new StringContent(activateResponse, Encoding.UTF8, "application/json");
                    response.StatusCode = HttpStatusCode.OK;
                }
                else if (request.RequestUri.ToString().OrdinalContains("operations/") && request.Method == HttpMethod.Patch && _failure)
                {
                    response.RequestMessage = new HttpRequestMessage();
                    response.RequestMessage.RequestUri = request.RequestUri;
                    response.StatusCode = HttpStatusCode.BadRequest;
                }
                else if (request.Method == HttpMethod.Delete)
                {
                    var operationLocation = "https://mockoperationlocation.com";
                    response = MockAsyncOperationHelper.AcceptedResponseWithOperationLocation(operationLocation);
                }
                else if (request.RequestUri.ToString().OrdinalContains("mockoperationlocation") && request.Method == HttpMethod.Get)
                {
                    response = MockAsyncOperationHelper.SuccessResponseWithSucceededStatus(new SubscriptionOperation());
                }

                return response;
            }
        }
    }
}
