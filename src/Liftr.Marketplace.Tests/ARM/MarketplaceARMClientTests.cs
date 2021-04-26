//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Models;
using Microsoft.Liftr.Marketplace.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using Microsoft.Liftr.Marketplace.Tests;
using Moq;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Marketplace.ARM.Tests
{
    public class MarketplaceARMClientTests
    {
        private readonly Uri _marketplaceEndpoint = new Uri("https://marketplaceapi.microsoft.com");
        private readonly string _version = "2018-08-31";
        private readonly MarketplaceARMClient _armClient;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;

        private readonly MarketplaceRequestMetadata _marketplaceRequestMetadata = new MarketplaceRequestMetadata()
        {
            MSClientTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47",
            MSClientObjectId = "c6ec8275-0d83-4f4e-88b9-be97b046785a",
            MSClientPrincipalId = "10030000A5D03A4B",
            MSClientIssuer = "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/",
            MSClientPrincipalName = "akagarw@microsoft.com",
        };

        private MarketplaceRestClient _marketplaceRestClient;

        public MarketplaceARMClientTests()
        {
            using var httpclient = new HttpClient();
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _marketplaceRestClient = new MarketplaceRestClient(_marketplaceEndpoint, _version, _httpClientFactory.Object, () => Task.FromResult("mockToken"));
            _armClient = new MarketplaceARMClient(_marketplaceRestClient);
        }

        [Fact]
        public void ARMClient_InvalidParameters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new MarketplaceARMClient(null));
            Assert.Throws<ArgumentNullException>(() => new MarketplaceARMClient(null, _marketplaceRestClient));
        }

        [Fact]
        public async Task CreateResourceAsync_Invalid_Parameters_Throws_Async()
        {
            var armClient = new MarketplaceARMClient(_marketplaceRestClient);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _armClient.CreateSaaSResourceAsync(new MarketplaceSaasResourceProperties(), _marketplaceRequestMetadata));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _armClient.CreateSaaSResourceAsync(CreateSaasResourceProperties("test"), new MarketplaceRequestMetadata()));
        }

        [Fact]
        public async Task CreateResourceAsync_Creates_resource_with_polling_Async()
        {
            var resourceName = $"test-{Guid.NewGuid()}";
            var saasResource = new BaseOperationResponse()
            {
                SubscriptionDetails = new MarketplaceSubscriptionDetails()
                {
                    Name = resourceName,
                    Id = Guid.NewGuid().ToString(),
                },
            };
            var marketplaceOfferDetail = CreateSaasResourceProperties(resourceName);
            var operationLocation = "https://mockoperationlocation.com";

            using var handler = new MockHttpMessageHandler(saasResource, operationLocation);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _marketplaceRestClient = new MarketplaceRestClient(_marketplaceEndpoint, _version, _httpClientFactory.Object, () => Task.FromResult("mockToken"));
            var armClient = new MarketplaceARMClient(_marketplaceRestClient);

            var createdResource = await armClient.CreateSaaSResourceAsync(marketplaceOfferDetail, _marketplaceRequestMetadata);

            Assert.Equal(createdResource.Id, saasResource.SubscriptionDetails.Id);
        }

        [Fact]
        public async Task CreateResourceAsync_Creates_resource_when_operation_returns_InProgress_Async()
        {
            var resourceName = $"test-{Guid.NewGuid()}";
            var marketplaceOfferDetail = CreateSaasResourceProperties(resourceName);

            var saasResource = new BaseOperationResponse()
            {
                SubscriptionDetails = new MarketplaceSubscriptionDetails()
                {
                    Name = resourceName,
                    Id = Guid.NewGuid().ToString(),
                },
                Status = OperationStatus.Succeeded,
            };

            var operationLocation = "https://mockoperationlocation.com";

            using var handler = new MockHttpMessageHandler(saasResource, operationLocation, true);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _marketplaceRestClient = new MarketplaceRestClient(_marketplaceEndpoint, _version, _httpClientFactory.Object, () => Task.FromResult("mockToken"));
            var armClient = new MarketplaceARMClient(_marketplaceRestClient);

            var createdResource = await armClient.CreateSaaSResourceAsync(marketplaceOfferDetail, _marketplaceRequestMetadata);

            Assert.Equal(createdResource.Id, saasResource.SubscriptionDetails.Id);
        }

        [Fact]
        public async Task CreateResourceAsync_Creates_resource_at_subscriptionLevel_with_polling_Async()
        {
            var resourceName = $"test-{Guid.NewGuid()}";
            var resourceGroup = "saas-test";
            var saasResource = new BaseOperationResponse()
            {
                SubscriptionDetails = new MarketplaceSubscriptionDetails()
                {
                    Name = resourceName,
                    Id = Guid.NewGuid().ToString(),
                },
            };
            var marketplaceOfferDetail = CreateSaasResourceProperties(resourceName);
            var operationLocation = "https://mockoperationlocation.com";

            using var handler = new MockHttpMessageHandler(saasResource, operationLocation);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _marketplaceRestClient = new MarketplaceRestClient(_marketplaceEndpoint, _version, _httpClientFactory.Object, () => Task.FromResult("mockToken"));
            var armClient = new MarketplaceARMClient(_marketplaceRestClient);

            var createdResource = await armClient.CreateSaaSResourceAsync(marketplaceOfferDetail, _marketplaceRequestMetadata, resourceGroup);

            Assert.Equal(createdResource.Id, saasResource.SubscriptionDetails.Id);
        }

        [Fact(Skip = "Regex Validation not implemented by Marketplace yet")]
        public async Task CreateResourceAsync_Creates_resource_at_subscriptionLevel_with_polling_Throws_Invalid_Regex_Exception_Async()
        {
            var resourceName = $"test-{Guid.NewGuid()}";
            var resourceGroup = "saas-test";
            var saasResource = new BaseOperationResponse()
            {
                SubscriptionDetails = new MarketplaceSubscriptionDetails()
                {
                    Name = resourceName,
                    Id = Guid.NewGuid().ToString(),
                },
            };
            var marketplaceOfferDetail = CreateSaasResourceProperties(resourceName);
            marketplaceOfferDetail.Name = "abcdAZH90~Org^._";
            var operationLocation = "https://mockoperationlocation.com";

            using var handler = new MockHttpMessageHandler(saasResource, operationLocation);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _marketplaceRestClient = new MarketplaceRestClient(_marketplaceEndpoint, _version, _httpClientFactory.Object, () => Task.FromResult("mockToken"));
            var armClient = new MarketplaceARMClient(_marketplaceRestClient);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await armClient.CreateSaaSResourceAsync(marketplaceOfferDetail, _marketplaceRequestMetadata, resourceGroup));
        }

        [Fact]
        public async Task CreateResourceAsync_Creates_resource_at_subscriptionLevel_when_operation_returns_InProgress_Async()
        {
            var resourceName = $"test-{Guid.NewGuid()}";
            var resourceGroup = "saas-test";
            var marketplaceOfferDetail = CreateSaasResourceProperties(resourceName);

            var saasResource = new BaseOperationResponse()
            {
                SubscriptionDetails = new MarketplaceSubscriptionDetails()
                {
                    Name = resourceName,
                    Id = Guid.NewGuid().ToString(),
                },
                Status = OperationStatus.Succeeded,
            };

            var operationLocation = "https://mockoperationlocation.com";

            using var handler = new MockHttpMessageHandler(saasResource, operationLocation, true);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _marketplaceRestClient = new MarketplaceRestClient(_marketplaceEndpoint, _version, _httpClientFactory.Object, () => Task.FromResult("mockToken"));
            var armClient = new MarketplaceARMClient(_marketplaceRestClient);

            var createdResource = await armClient.CreateSaaSResourceAsync(marketplaceOfferDetail, _marketplaceRequestMetadata, resourceGroup);

            Assert.Equal(createdResource.Id, saasResource.SubscriptionDetails.Id);
        }

        [Fact]
        public async Task DeleteSaaSResourceAsync_Deletes_resource_at_subscriptionLevel_with_polling_Async()
        {
            var resourceName = $"test-{Guid.NewGuid()}";
            var resourceGroup = "saas-test";
            var subscription = "subscription";
            var operationLocation = "https://deleteoperationlocation.com";

            var subsOperation = new SubscriptionOperation()
            {
                Action = "Unsubscribe",
                MarketplaceSubscription = new MarketplaceSubscription(Guid.NewGuid()),
                PlanId = "PAYG",
                Status = OperationStatus.Succeeded,
            };

            using var handler = new MockHttpMessageHandler(null, operationLocation, subOperation: subsOperation);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _marketplaceRestClient = new MarketplaceRestClient(_marketplaceEndpoint, _version, _httpClientFactory.Object, () => Task.FromResult("mockToken"));
            var armClient = new MarketplaceARMClient(_marketplaceRestClient);

            Func<Task> act = async () => await armClient.DeleteSaaSResourceAsync(subscription, resourceName, resourceGroup, _marketplaceRequestMetadata);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DeleteSaaSResourceAsync_Deletes_resource_at_subscriptionLevel_when_operation_returns_InProgress_Async()
        {
            var resourceName = $"test-{Guid.NewGuid()}";
            var resourceGroup = "saas-test";
            var subscription = "subscription";
            var operationLocation = "https://deleteoperationlocation.com";

            var subsOperation = new SubscriptionOperation()
            {
                Action = "Unsubscribe",
                MarketplaceSubscription = new MarketplaceSubscription(Guid.NewGuid()),
                PlanId = "PAYG",
                Status = OperationStatus.Succeeded,
            };

            using var handler = new MockHttpMessageHandler(null, operationLocation, subOperation: subsOperation);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _marketplaceRestClient = new MarketplaceRestClient(_marketplaceEndpoint, _version, _httpClientFactory.Object, () => Task.FromResult("mockToken"));
            var armClient = new MarketplaceARMClient(_marketplaceRestClient);

            Func<Task> act = async () => await armClient.DeleteSaaSResourceAsync(subscription, resourceName, resourceGroup, _marketplaceRequestMetadata);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ValidatesSaaSPurchasePaymentAsync_Validates_SaaS_Purchase_Payment_Async()
        {
            var paymentValidationRequest = new PaymentValidationRequest()
            {
                TenantId = "tenantId",
                AzureSubscriptionId = "subscription1",
                PlanId = "PAYG",
                OfferId = "offer",
                TermId = "hdcbvgdjk",
                PublisherId = "confluent",
                Email = "rohanand@microsoft.com",
            };

            var path = "paymentValidation";

            using var handler = new MockHttpMessageHandler(null, path);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _marketplaceRestClient = new MarketplaceRestClient(_marketplaceEndpoint, _version, _httpClientFactory.Object, () => Task.FromResult("mockToken"));
            var armClient = new MarketplaceARMClient(_marketplaceRestClient);

            var validationResponse = await armClient.ValidatesSaaSPurchasePaymentAsync(paymentValidationRequest, _marketplaceRequestMetadata);
            Assert.True(validationResponse.IsSuccess);
        }

        /* [Fact(Skip = "Not implemented")]
        public async Task InitiateDeleteResource_throws_exception_if_resource_deletion_fails_Async()
        {
            var resourceId = $"/providers/Microsoft.SaaS/saasresources/{Guid.NewGuid()}";
            var armClient = new MarketplaceARMClient(_logger, _marketplaceRestClient);

            var resourceToDelete = new MarketplaceSaasResource(MarketplaceSubscription.From("test"), "name", "planId", "termid", BillingTermTypes.Monthly);

            Func<Task> act = async () => await armClient.DeleteResourceAsync(resourceToDelete, _marketplaceRequestMetadata);
            await act.Should().ThrowAsync<MarketplaceARMException>();
        }

        [Fact(Skip = "Not implemented")]
        public async Task InitiateDeleteResource_initiates_request_to_delete_resource_Async()
        {
            var resourceToDelete = new MarketplaceSaasResource(MarketplaceSubscription.From("test"), "name", "planId", "termid", BillingTermTypes.Monthly);
            var armClient = new MarketplaceARMClient(_logger, _marketplaceRestClient);

            Func<Task> act = async () => await armClient.DeleteResourceAsync(resourceToDelete, _marketplaceRequestMetadata);
            await act.Should().NotThrowAsync();
        } */

        private static MarketplaceSaasResourceProperties CreateSaasResourceProperties(string resourceName)
        {
            return new MarketplaceSaasResourceProperties()
            {
                PublisherId = "isvtestuklegacy",
                OfferId = "liftr_saas_offer",
                Name = resourceName,
                PlanId = "liftsaasplan1",
                PaymentChannelType = "SubscriptionDelegated",
                TermId = "hjdtn7tfnxcy",
                PaymentChannelMetadata = new PaymentChannelMetadata()
                {
                    AzureSubscriptionId = "f5f53739-49e7-49a4-b5b1-00c63a7961a1",
                },
            };
        }

        internal class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly BaseOperationResponse _creationResponse;
            private readonly string _operationLocation;
            private readonly SubscriptionOperation _subOperation;
            private bool _progress;

            public MockHttpMessageHandler(BaseOperationResponse creationResponse, string operationLocation, bool progress = false, SubscriptionOperation subOperation = null)
            {
                _creationResponse = creationResponse;
                _operationLocation = operationLocation;
                _progress = progress;
                _subOperation = subOperation;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Yield();
                var response = new HttpResponseMessage();

                if (request.RequestUri.ToString().OrdinalContains("saasresources") && request.Method == HttpMethod.Put)
                {
                    response = MockAsyncOperationHelper.AcceptedResponseWithOperationLocation(_operationLocation);
                }
                else if (request.RequestUri.ToString().OrdinalContains("resources") && request.Method == HttpMethod.Put)
                {
                    response = MockAsyncOperationHelper.AcceptedResponseWithOperationLocation(_operationLocation);
                }
                else if (request.RequestUri.ToString().OrdinalContains("resources") && request.Method == HttpMethod.Delete)
                {
                    response = MockAsyncOperationHelper.AcceptedResponseWithOperationLocation(_operationLocation);
                }
                else if (request.RequestUri.ToString().OrdinalContains("mockoperationlocation") && request.Method == HttpMethod.Get && !_progress)
                {
                    response = MockAsyncOperationHelper.SuccessResponseWithSucceededStatus(_creationResponse);
                }
                else if (request.RequestUri.ToString().OrdinalContains("deleteoperationlocation") && request.Method == HttpMethod.Get && !_progress)
                {
                    response = MockAsyncOperationHelper.SuccessResponseWithSucceededStatus(_subOperation);
                }
                else if (request.RequestUri.ToString().OrdinalContains("mockoperationlocation") && request.Method == HttpMethod.Get && _progress)
                {
                    _progress = false;
                    response = MockAsyncOperationHelper.SuccessResponseWithInProgressStatus();
                }
                else if (request.RequestUri.ToString().OrdinalContains("paymentValidation") && request.Method == HttpMethod.Post)
                {
                    response.StatusCode = System.Net.HttpStatusCode.OK;
                    var content = "Payment Validated!";
                    var responseContent = content.ToJson();
                    response.Content = new StringContent(responseContent);
                }

                return response;
            }
        }
    }
}
