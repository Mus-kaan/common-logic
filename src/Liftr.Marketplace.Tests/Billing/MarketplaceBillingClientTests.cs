﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr.Marketplace.Billing;
using Microsoft.Liftr.Marketplace.Billing.Exceptions;
using Microsoft.Liftr.Marketplace.Billing.Models;
using Microsoft.Liftr.Marketplace.Options;
using Microsoft.Liftr.Marketplace.Saas.Options;
using Moq;
using Newtonsoft.Json;
using Serilog;
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
        private readonly Mock<ILogger> _mockLogger;
        private MarketplaceSaasOptions _marketplaceOptions;

        public MarketplaceBillingClientTests()
        {
            _mockLogger = new Mock<ILogger>();

            SetupLogger();
            SetupMarketplaceSaasOptions();
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_To_Marketplace_Success_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

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

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            var response = await billingClient.SendUsageEventAsync(request, CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_To_Marketplace_BadRequest_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();

            var request = new UsageEventRequest
            {
                ResourceId = string.Empty,
                Quantity = 4,
                Dimension = "Meter1",
                EffectiveStartTime = DateTime.Now.AddHours(-1),
                PlanId = "Basic",
            };

            var expectedResponse = MockMeteredBillingBadRequestResponse("One or more error occured.", "ReosourceId is invalid.", Constants.UsageEventPath, "ResourceId");

            using var handler = new MockHttpMessageHandler(expectedResponse);
            using var httpClient = new HttpClient(handler, false);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            var response = await billingClient.SendUsageEventAsync(request, CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_To_Marketplace_Forbidden_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

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

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            var response = await billingClient.SendUsageEventAsync(request, CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_To_Marketplace_Conflict_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

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

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            var response = await billingClient.SendUsageEventAsync(request, CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Batch_Usage_Event_To_Marketplace_Success_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

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

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            var response = await billingClient.SendBatchUsageEventAsync(request, CancellationToken.None);

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
                        ResourceId = string.Empty,
                        Quantity = 4,
                        Dimension = "Meter1",
                        EffectiveStartTime = DateTime.Now.AddHours(-1),
                        PlanId = "Basic",
                    },
                },
            };

            var expectedResponse = MockMeteredBillingBadRequestResponse("One or more error occured.", "ReosourceId is invalid.", Constants.UsageEventPath, "ResourceId");

            using var handler = new MockHttpMessageHandler(expectedResponse);
            using var httpClient = new HttpClient(handler, false);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            var response = await billingClient.SendBatchUsageEventAsync(request, CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Batch_Usage_Event_To_Marketplace_Forbidden_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

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

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            var response = await billingClient.SendBatchUsageEventAsync(request, CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task BillingClient_Send_Batch_Usage_Event_To_Marketplace_Conflict_Response_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

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

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            var response = await billingClient.SendBatchUsageEventAsync(request, CancellationToken.None);

            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public void BillingClient_Constructor_ThrowNullArgument_Exception()
        {
            var marketplaceOptionException = Assert.Throws<ArgumentNullException>(() => new MarketplaceBillingClient(null, () => Task.FromResult("mockToken"), _mockLogger.Object, null));
            var tokenCallBackException = Assert.Throws<ArgumentNullException>(() => new MarketplaceBillingClient(_marketplaceOptions, null, _mockLogger.Object, null));
            var loggerException = Assert.Throws<ArgumentNullException>(() => new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), null, null));
            var httpClientException = Assert.Throws<ArgumentNullException>(() => new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, null));

            Assert.Equal("Value cannot be null. (Parameter 'marketplaceOptions')", marketplaceOptionException.Message);
            Assert.Equal("Value cannot be null. (Parameter 'authenticationTokenCallback')", tokenCallBackException.Message);
            Assert.Equal("Value cannot be null. (Parameter 'logger')", loggerException.Message);
            Assert.Equal("Value cannot be null. (Parameter 'client')", httpClientException.Message);
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_WebException_Exception_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

            var request = new UsageEventRequest
            {
                ResourceId = resourceId,
                Quantity = 4,
                Dimension = "Meter1",
                EffectiveStartTime = DateTime.Now.AddHours(-1),
                PlanId = "Basic",
            };

            var expectedResponse = MockMeteredBillingSuccessResponse(resourceId);

            using var handler = new MockHttpMessageHandlerThrowWebException();
            using var httpClient = new HttpClient(handler, false);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            await Assert.ThrowsAsync<MarketplaceBillingException>(async () => await billingClient.SendUsageEventAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task BillingClient_Send_Usage_Event_MarketplaceBillingException_Exception_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

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

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            await Assert.ThrowsAsync<MarketplaceBillingException>(async () => await billingClient.SendUsageEventAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task BillingClient_Send_Batch_Usage_Event_WebException_Exception_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

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

            using var handler = new MockHttpMessageHandlerThrowWebException();
            using var httpClient = new HttpClient(handler, false);

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            await Assert.ThrowsAsync<MarketplaceBillingException>(async () => await billingClient.SendBatchUsageEventAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task BillingClient_Send_Batch_Usage_Event_MarketplaceBillingException_Exception_Async()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

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

            var billingClient = new MarketplaceBillingClient(_marketplaceOptions, () => Task.FromResult("mockToken"), _mockLogger.Object, httpClient);
            await Assert.ThrowsAsync<MarketplaceBillingException>(async () => await billingClient.SendBatchUsageEventAsync(request, CancellationToken.None));
        }

        private void SetupLogger()
        {
            _mockLogger.Setup(log => log.Information(It.IsAny<string>(), It.IsAny<string>()));
        }

        private void SetupMarketplaceSaasOptions()
        {
            _marketplaceOptions = new MarketplaceSaasOptions
            {
                API = new MarketplaceAPIOptions
                {
                    ApiVersion = "2018-09-15",
                    Endpoint = new Uri("https://marketplaceapi.microsoft.com/api"),
                },
            };
        }

        private MeteredBillingSuccessResponse MockMeteredBillingSuccessResponse(string resourceId)
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

        private MeteredBillingConflictResponse MockMeteredBillingConflictResponse(string resourceId)
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

        private MeteredBillingBatchUsageSuccessResponse MockMeteredBillingBatchSuccessResponse(string resourceId)
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

        private class MockHttpMessageHandlerThrowWebException : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Yield();

                throw new WebException();
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