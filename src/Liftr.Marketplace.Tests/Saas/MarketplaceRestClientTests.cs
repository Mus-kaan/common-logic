//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr.Marketplace.Contracts;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Tests;
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

namespace Microsoft.Liftr.Marketplace.Saas.Tests
{
    public class MarketplaceRestClientTests
    {
        private const string ApiVersion = "test-version";
        private readonly Uri _endpoint = new Uri("https://testmock.com/api");
        private MarketplaceRestClient _marketplaceRestClient;
        private ILogger _logger;

        public MarketplaceRestClientTests()
        {
            _logger = new Mock<ILogger>().Object;
        }

        [Fact]
        public async Task Calls_the_apis_with_the_headers_and_authorization_Async()
        {
            var expectedResponse = "headerAndAuthorization";
            using var handler = new MockHttpMessageHandler();
            using var httpClient = new HttpClient(handler, false);
            _marketplaceRestClient = new MarketplaceRestClient(_endpoint, ApiVersion, _logger, httpClient, () => Task.FromResult("mockToken"));

            var actualResponse = await _marketplaceRestClient.SendRequestAsync<string>(HttpMethod.Post, "/path/test", content: "somecontent");
            Assert.Equal(expectedResponse, actualResponse);
        }

        [Fact]
        public async Task Adds_the_additional_headers_to_the_request_Async()
        {
            var expectedResponse = "additionalHeaderAndAuthorization";
            using var handler = new MockHttpMessageHandler(true);
            using var httpClient = new HttpClient(handler, false);
            _marketplaceRestClient = new MarketplaceRestClient(_endpoint, ApiVersion, _logger, httpClient, () => Task.FromResult("mockToken"));

            Dictionary<string, string> additionalHeaders = new Dictionary<string, string>() { { "test_additional_header", "value" } };
            var actualResponse = await _marketplaceRestClient.SendRequestAsync<string>(HttpMethod.Post, "/path/test", additionalHeaders: additionalHeaders, content: "somecontent");

            Assert.Equal(expectedResponse, actualResponse);
        }

        [Fact]
        public async Task Polls_the_operation_when_accepted_progress_success_response_is_received_Async()
        {
            using var handler = new MockHttpMessageHandler(progress: true);
            using var httpClient = new HttpClient(handler, false);
            _marketplaceRestClient = new MarketplaceRestClient(_endpoint, ApiVersion, _logger, httpClient, () => Task.FromResult("mockToken"));

            Func<Task> act = async () => { await _marketplaceRestClient.SendRequestWithPollingAsync<TestResource>(HttpMethod.Put, "/path/test", content: "somecontent"); };
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendRequestWithPollingAsync_adds_the_additional_headers_to_subrequest_Async()
        {
            using var handler = new MockHttpMessageHandler(extraHeader: true, progress: true);
            using var httpClient = new HttpClient(handler, false);
            _marketplaceRestClient = new MarketplaceRestClient(_endpoint, ApiVersion, _logger, httpClient, () => Task.FromResult("mockToken"));

            var additionalHeader = new Dictionary<string, string>
            {
                { "key", "value" },
            };

            Func<Task> act = async () => { await _marketplaceRestClient.SendRequestWithPollingAsync<TestResource>(HttpMethod.Put, "/path/test", content: "somecontent", additionalHeaders: additionalHeader); };
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendRequestWithPollingAsync_throws_exception_if_operation_location_is_not_set_Async()
        {
            using var handler = new MockHttpMessageHandler(extraHeader: true, progress: true);
            using var httpClient = new HttpClient(handler, false);
            _marketplaceRestClient = new MarketplaceRestClient(_endpoint, ApiVersion, _logger, httpClient, () => Task.FromResult("mockToken"));

            await Assert.ThrowsAsync<MarketplaceHttpException>(async () => await _marketplaceRestClient.SendRequestWithPollingAsync<TestResource>(HttpMethod.Put, "/operation/test", content: "somecontent"));
        }

        [Fact]
        public async Task SendRequestWithPollingAsync_throws_exception_if_retry_value_is_not_set_Async()
        {
            using var handler = new MockHttpMessageHandler(extraHeader: true, progress: true);
            using var httpClient = new HttpClient(handler, false);
            _marketplaceRestClient = new MarketplaceRestClient(_endpoint, ApiVersion, _logger, httpClient, () => Task.FromResult("mockToken"));

            await Assert.ThrowsAsync<MarketplaceHttpException>(async () => await _marketplaceRestClient.SendRequestWithPollingAsync<TestResource>(HttpMethod.Put, "/retry/test", content: "somecontent"));
        }

        internal class TestResource : MarketplaceAsyncOperationResponse
        {
        }

        internal class MockHttpMessageHandler : HttpMessageHandler
        {
            private bool _extraHeader;
            private bool _progress;

            public MockHttpMessageHandler(bool extraHeader = false, bool progress = false)
            {
                _extraHeader = extraHeader;
                _progress = progress;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Yield();
                var response = new HttpResponseMessage();

                if (request.RequestUri.ToString().OrdinalContains("path") && request.Method == HttpMethod.Post && request.Headers.Contains("x-ms-requestid") && request.Headers.Contains("authorization") && !_extraHeader)
                {
                    var sendRequestContent = "headerAndAuthorization";
                    var sendRequestContentResponse = JsonConvert.SerializeObject(sendRequestContent);
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content = new StringContent(sendRequestContentResponse, Encoding.UTF8, "application/json");
                }
                else if (request.RequestUri.ToString().OrdinalContains("path") && request.Method == HttpMethod.Post && request.Headers.Contains("x-ms-requestid") && request.Headers.Contains("authorization") && request.Headers.Contains("test_additional_header") && _extraHeader)
                {
                    var sendRequestContent = "additionalHeaderAndAuthorization";
                    var sendRequestContentResponse = JsonConvert.SerializeObject(sendRequestContent);
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content = new StringContent(sendRequestContentResponse, Encoding.UTF8, "application/json");
                }
                else if (request.RequestUri.ToString().OrdinalContains("path") && request.Method == HttpMethod.Put && !_extraHeader)
                {
                    var operationLocation = "https://mockoperationlocation.com";
                    response = MockAsyncOperationHelper.AcceptedResponseWithOperationLocation(operationLocation);
                }
                else if (request.RequestUri.ToString().OrdinalContains("mockoperationlocation") && request.Method == HttpMethod.Get && !_progress && !_extraHeader)
                {
                    response = MockAsyncOperationHelper.SuccessResponseWithSucceededStatus(new TestResource());
                }
                else if (request.RequestUri.ToString().OrdinalContains("mockoperationlocation") && request.Method == HttpMethod.Get && _progress && !_extraHeader)
                {
                    _progress = false;
                    response = MockAsyncOperationHelper.SuccessResponseWithInProgressStatus();
                }
                else if (request.RequestUri.ToString().OrdinalContains("path") && request.Method == HttpMethod.Put && request.Headers.Contains("key") && _extraHeader)
                {
                    var operationLocation = "https://mockoperationlocation.com";
                    response = MockAsyncOperationHelper.AcceptedResponseWithOperationLocation(operationLocation);
                }
                else if (request.RequestUri.ToString().OrdinalContains("mockoperationlocation") && request.Method == HttpMethod.Get && request.Headers.Contains("key") && !_progress && _extraHeader)
                {
                    response = MockAsyncOperationHelper.SuccessResponseWithSucceededStatus(new TestResource());
                }
                else if (request.RequestUri.ToString().OrdinalContains("mockoperationlocation") && request.Method == HttpMethod.Get && request.Headers.Contains("key") && _progress && _extraHeader)
                {
                    _progress = false;
                    response = MockAsyncOperationHelper.SuccessResponseWithInProgressStatus();
                }
                else if (request.RequestUri.ToString().OrdinalContains("operation") && request.Method == HttpMethod.Put)
                {
                    response = MockAsyncOperationHelper.AcceptedResponseWithoutOperationLocation();
                }
                else if (request.RequestUri.ToString().OrdinalContains("retry") && request.Method == HttpMethod.Put)
                {
                    var operationLocation = "https://mockoperationlocation.com";
                    response = MockAsyncOperationHelper.AcceptedResponseWithoutRetryAfter(operationLocation);
                }

                return response;
            }
        }
    }
}
