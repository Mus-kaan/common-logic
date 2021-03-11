//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr.Marketplace.Billing;
using Microsoft.Liftr.Marketplace.Billing.Exceptions;
using Microsoft.Liftr.Marketplace.Billing.Models;
using Microsoft.Liftr.Marketplace.Options;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Marketplace.Tests.Billing
{
    public class MarketplaceBillingClientTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private MarketplaceAPIOptions _marketplaceOptions;

        public MarketplaceBillingClientTests()
        {
            _httpClientFactory = new Mock<IHttpClientFactory>();
            SetupMarketplaceSaasOptions();
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_To_Marketplace_Success_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid();

            var request = new UsageEventRequest
            {
                ResourceId = resourceId,
                Quantity = 4,
                Dimension = "Meter1",
                EffectiveStartTime = DateTime.Now.AddHours(-1),
                PlanId = "Basic",
            };

            var expectedResponse = MockMeteredBillingSuccessResponse(resourceId);

            using var handler = new MockHttpMessageHandler(expectedResponse);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _httpClientFactory.Object);
            var response = await billingClient.SendUsageEventAsync(request, cancellationToken: CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_To_Marketplace_BadRequest_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();

            var request = new UsageEventRequest
            {
                ResourceId = Guid.Empty,
                Quantity = 4,
                Dimension = "Meter1",
                EffectiveStartTime = DateTime.Now.AddHours(-1),
                PlanId = "Basic",
            };

            var expectedResponse = MockMeteredBillingBadRequestResponse("One or more error occured.", "ReosourceId is invalid.", MarketplaceConstants.BillingUsageEventPath, "ResourceId");

            using var handler = new MockHttpMessageHandler(expectedResponse);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _httpClientFactory.Object);
            var response = await billingClient.SendUsageEventAsync(request, cancellationToken: CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_To_Marketplace_Forbidden_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid();

            var request = new UsageEventRequest
            {
                ResourceId = resourceId,
                Quantity = 4,
                Dimension = "Meter1",
                EffectiveStartTime = DateTime.Now.AddHours(-1),
                PlanId = "Basic",
            };

            var expectedResponse = MockMeteredBillingForbiddenResponse("User is not allowed authorized to call this.");

            using var handler = new MockHttpMessageHandler(expectedResponse);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _httpClientFactory.Object);
            var response = await billingClient.SendUsageEventAsync(request, cancellationToken: CancellationToken.None);

            response.RawResponse.Should().BeEquivalentTo(expectedResponse.RawResponse);
            response.StatusCode.Should().BeEquivalentTo(expectedResponse.StatusCode);
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_To_Marketplace_Conflict_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid();

            var request = new UsageEventRequest
            {
                ResourceId = resourceId,
                Quantity = 4,
                Dimension = "Meter1",
                EffectiveStartTime = DateTime.Now,
                PlanId = "Basic",
            };

            var expectedResponse = MockMeteredBillingConflictResponse(resourceId);

            using var handler = new MockHttpMessageHandler(expectedResponse);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _httpClientFactory.Object);
            var response = await billingClient.SendUsageEventAsync(request, cancellationToken: CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Batch_Usage_Event_To_Marketplace_Success_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid();

            var request = new BatchUsageEventRequest
            {
                Request = new List<UsageEventRequest>
                {
                    new UsageEventRequest
                    {
                        ResourceId = resourceId,
                        Quantity = 4,
                        Dimension = "Meter1",
                        EffectiveStartTime = DateTime.Now.AddHours(-1),
                        PlanId = "Basic",
                    },
                },
            };

            var expectedResponse = MockMeteredBillingBatchSuccessResponse(resourceId);

            using var handler = new MockHttpMessageHandler(expectedResponse);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _httpClientFactory.Object);
            var response = await billingClient.SendBatchUsageEventAsync(request, cancellationToken: CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Batch_Usage_Event_To_Marketplace_BadRequest_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();

            var request = new BatchUsageEventRequest
            {
                Request = new List<UsageEventRequest>
                {
                    new UsageEventRequest
                    {
                        ResourceId = Guid.Empty,
                        Quantity = 4,
                        Dimension = "Meter1",
                        EffectiveStartTime = DateTime.Now.AddHours(-1),
                        PlanId = "Basic",
                    },
                },
            };

            var expectedResponse = MockMeteredBillingBadRequestResponse("One or more error occured.", "ReosourceId is invalid.", MarketplaceConstants.BillingBatchUsageEventPath, "ResourceId");

            using var handler = new MockHttpMessageHandler(expectedResponse);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _httpClientFactory.Object);
            var response = await billingClient.SendBatchUsageEventAsync(request, cancellationToken: CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Batch_Usage_Event_To_Marketplace_Forbidden_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid();

            var request = new BatchUsageEventRequest
            {
                Request = new List<UsageEventRequest>
                {
                    new UsageEventRequest
                    {
                        ResourceId = resourceId,
                        Quantity = 4,
                        Dimension = "Meter1",
                        EffectiveStartTime = DateTime.Now.AddHours(-1),
                        PlanId = "Basic",
                    },
                },
            };

            var expectedResponse = MockMeteredBillingForbiddenResponse("User is not allowed authorized to call this.");

            using var handler = new MockHttpMessageHandler(expectedResponse);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _httpClientFactory.Object);
            var response = await billingClient.SendBatchUsageEventAsync(request, cancellationToken: CancellationToken.None);

            response.RawResponse.Should().BeEquivalentTo(expectedResponse.RawResponse);
            response.StatusCode.Should().BeEquivalentTo(expectedResponse.StatusCode);
        }

        [Fact]
        public async Task BillingClient_Send_Batch_Usage_Event_To_Marketplace_Conflict_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid();

            var request = new BatchUsageEventRequest
            {
                Request = new List<UsageEventRequest>
                {
                    new UsageEventRequest
                    {
                        ResourceId = resourceId,
                        Quantity = 4,
                        Dimension = "Meter1",
                        EffectiveStartTime = DateTime.Now.AddHours(0),
                        PlanId = "Basic",
                    },
                },
            };

            var expectedResponse = MockMeteredBillingConflictResponse(resourceId);

            using var handler = new MockHttpMessageHandler(expectedResponse);
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _httpClientFactory.Object);
            var response = await billingClient.SendBatchUsageEventAsync(request, cancellationToken: CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public void BillingClient_Constructor_ThrowNullArgument_Exception()
        {
            var marketplaceOptionException = Assert.Throws<ArgumentNullException>(() => new MarketplaceBillingClient(null, () => Task.FromResult("mockToken"), null));
            var tokenCallBackException = Assert.Throws<ArgumentNullException>(() => new MarketplaceBillingClient(_marketplaceOptions, null, null));
            var loggerException = Assert.Throws<ArgumentNullException>(() => new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), null, null));
            var httpClientException = Assert.Throws<ArgumentNullException>(() => new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), null));

            Assert.Equal("Value cannot be null. (Parameter 'marketplaceOptions')", marketplaceOptionException.Message);
            Assert.Equal("Value cannot be null. (Parameter 'authenticationTokenCallback')", tokenCallBackException.Message);
            Assert.Equal("Value cannot be null. (Parameter 'logger')", loggerException.Message);
            Assert.Equal("Value cannot be null. (Parameter 'httpClientFactory')", httpClientException.Message);
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_MarketplaceBillingException_Exception_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid();

            var request = new UsageEventRequest
            {
                ResourceId = resourceId,
                Quantity = 4,
                Dimension = "Meter1",
                EffectiveStartTime = DateTime.Now.AddHours(-1),
                PlanId = "Basic",
            };

            var expectedResponse = MockMeteredBillingSuccessResponse(resourceId);

            using var handler = new MockHttpMessageHandlerThrowMarketplaceBillingException();
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _httpClientFactory.Object);
            await Assert.ThrowsAsync<MarketplaceBillingException>(async () => await billingClient.SendUsageEventAsync(request, cancellationToken: CancellationToken.None));
        }

        [Fact]
        public async Task BillingClient_Send_Batch_Usage_Event_MarketplaceBillingException_Exception_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid();

            var request = new BatchUsageEventRequest
            {
                Request = new List<UsageEventRequest>
                {
                    new UsageEventRequest
                    {
                        ResourceId = resourceId,
                        Quantity = 4,
                        Dimension = "Meter1",
                        EffectiveStartTime = DateTime.Now.AddHours(-1),
                        PlanId = "Basic",
                    },
                },
            };

            var expectedResponse = MockMeteredBillingSuccessResponse(resourceId);

            using var handler = new MockHttpMessageHandlerThrowMarketplaceBillingException();
            using var httpClient = new HttpClient(handler, false);
            _httpClientFactory.Setup(client => client.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _httpClientFactory.Object);
            await Assert.ThrowsAsync<MarketplaceBillingException>(async () => await billingClient.SendBatchUsageEventAsync(request, cancellationToken: CancellationToken.None));
        }

        private void SetupMarketplaceSaasOptions()
        {
            _marketplaceOptions = new MarketplaceAPIOptions
            {
                ApiVersion = "2018-09-15",
                Endpoint = new Uri("https://marketplaceapi.microsoft.com/api"),
            };
        }

        private MeteredBillingSuccessResponse MockMeteredBillingSuccessResponse(Guid resourceId)
        {
            var response = new MeteredBillingSuccessResponse
            {
                Success = true,
                Code = "Ok",
                UsageEventId = Guid.NewGuid(),
                Status = "Accepted",
                MessageTime = DateTime.Now,
                ResourceId = resourceId,
                Quantity = 4,
                Dimension = "Meter1",
                EffectiveStartTime = DateTime.Now.AddMinutes(-10),
                PlanId = "Basic",
                StatusCode = HttpStatusCode.OK,
            };

            response.RawResponse = JsonConvert.SerializeObject(response);
            return response;
        }

        private MeteredBillingBadRequestResponse MockMeteredBillingBadRequestResponse(string message, string detailMessage, string target, string detailTarget)
        {
            var response = new MeteredBillingBadRequestResponse
            {
                Success = false,
                Code = "Badargument",
                Message = message,
                Details = new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        Code = "Badargument",
                        Message = detailMessage,
                        Target = detailTarget,
                    },
                },
                Target = target,
                StatusCode = HttpStatusCode.BadRequest,
            };

            response.RawResponse = JsonConvert.SerializeObject(response);
            return response;
        }

        private MeteredBillingForbiddenResponse MockMeteredBillingForbiddenResponse(string message)
        {
            var response = new MeteredBillingForbiddenResponse
            {
                Code = "Forbidden",
                Message = message,
                StatusCode = HttpStatusCode.Forbidden,
                Success = false,
            };

            response.RawResponse = JsonConvert.SerializeObject(response);
            return response;
        }

        private MeteredBillingConflictResponse MockMeteredBillingConflictResponse(Guid resourceId)
        {
            var response = new MeteredBillingConflictResponse
            {
                Success = false,
                Code = "Conflict",
                AdditionalInfo = new AdditionalInfo
                {
                    AcceptedMessage = new AcceptedMessage
                    {
                        UsageEventId = Guid.NewGuid(),
                        Status = "Accepted",
                        MessageTime = DateTime.Now,
                        ResourceId = resourceId,
                        Quantity = 4,
                        Dimension = "Meter1",
                        EffectiveStartTime = DateTime.Now.AddMinutes(-10),
                        PlanId = "Basic",
                    },
                },
                Message = "This usage event already exist.",
                StatusCode = HttpStatusCode.Conflict,
            };

            response.RawResponse = JsonConvert.SerializeObject(response);
            return response;
        }

        private MeteredBillingBatchUsageSuccessResponse MockMeteredBillingBatchSuccessResponse(Guid resourceId)
        {
            var response = new MeteredBillingBatchUsageSuccessResponse
            {
                Code = "Ok",
                Count = 1,
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Result = new List<BatchAdditionInfoModel>
                {
                    new BatchAdditionInfoModel
                    {
                        UsageEventId = Guid.NewGuid(),
                        Status = "Accepted",
                        MessageTime = DateTime.Now,
                        ResourceId = resourceId,
                        Quantity = 4,
                        Dimension = "Meter1",
                        EffectiveStartTime = DateTime.Now.AddMinutes(-10),
                        PlanId = "Basic",
                    },
                },
            };

            response.RawResponse = JsonConvert.SerializeObject(response);
            return response;
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly MeteredBillingRequestResponse _mockResponse;

            public MockHttpMessageHandler(MeteredBillingRequestResponse response)
            {
                _mockResponse = response;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Yield();

                return new HttpResponseMessage
                {
                    StatusCode = _mockResponse.StatusCode,
                    Content = new StringContent(_mockResponse.RawResponse, Encoding.UTF8, "application/json"),
                };
            }
        }

        private class MockHttpMessageHandlerThrowMarketplaceBillingException : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Yield();

                throw new MarketplaceBillingException();
            }
        }
    }
}
