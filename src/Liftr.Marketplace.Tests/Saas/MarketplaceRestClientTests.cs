//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Flurl.Http.Testing;
using Microsoft.Liftr.Marketplace.Contracts;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Tests;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Marketplace.Saas.Tests
{
    public class MarketplaceRestClientTests
    {
        private const string ApiVersion = "test-version";
        private readonly MarketplaceRestClient _marketplaceRestClient;
        private readonly Uri _endpoint = new Uri("https://testmock.com/api");

        public MarketplaceRestClientTests()
        {
            var logger = new Mock<ILogger>().Object;
            _marketplaceRestClient = new MarketplaceRestClient(_endpoint, ApiVersion, logger, () => Task.FromResult("mockToken"));
        }

        [Fact]
        public async Task Calls_the_apis_with_the_headers_and_authorization_Async()
        {
            using var httpTest = new HttpTest();

            var response = await _marketplaceRestClient.SendRequestAsync<string>(HttpMethod.Post, "/path/test", content: "somecontent");
            httpTest.ShouldHaveCalled($"{_endpoint}/path/test")
                   .WithVerb(HttpMethod.Post)
                   .WithContentType("application/json")
                   .WithHeader("x-ms-requestid")
                   .WithHeader("authorization")
                   .WithQueryParamValue("api-version", ApiVersion)
                   .Times(1);
        }

        [Fact]
        public async Task Adds_the_additional_headers_to_the_request_Async()
        {
            using var httpTest = new HttpTest();
            var logger = new Mock<ILogger>().Object;

            var requestId = Guid.NewGuid();

            Dictionary<string, string> additionalHeaders = new Dictionary<string, string>() { { "test_additional_header", "value" } };
            var response = await _marketplaceRestClient.SendRequestAsync<string>(HttpMethod.Post, "/path/test", additionalHeaders: additionalHeaders, content: "somecontent");

            httpTest.ShouldHaveCalled($"{_endpoint}/path/test")
                   .WithHeader("test_additional_header", "value");
        }

        [Fact]
        public async Task Polls_the_operation_when_accepted_response_is_received_Async()
        {
            using var httpTest = new HttpTest();
            var operationLocation = "https://mockoperationlocation.com";
            using var response1 = MockAsyncOperationHelper.AcceptedResponseWithOperationLocation(operationLocation);
            using var response2 = MockAsyncOperationHelper.SuccessResponseWithInProgressStatus();
            using HttpResponseMessage response3 = MockAsyncOperationHelper.SuccessResponseWithSucceededStatus(new TestResource());

            httpTest.ResponseQueue.Enqueue(response1);
            httpTest.ResponseQueue.Enqueue(response2);
            httpTest.ResponseQueue.Enqueue(response3);

            var response = await _marketplaceRestClient.SendRequestWithPollingAsync<TestResource>(HttpMethod.Put, "/path/test", content: "somecontent");

            httpTest.ShouldHaveCalled(operationLocation)
                .WithVerb(HttpMethod.Get)
                .Times(2);

            httpTest.ShouldHaveCalled($"{_endpoint}/path/test")
                .WithVerb(HttpMethod.Put)
                .Times(1);
        }

        [Fact]
        public async Task SendRequestWithPollingAsync_adds_the_additional_headers_to_subrequest_Async()
        {
            using var httpTest = new HttpTest();
            var operationLocation = "https://mockoperationlocation.com";
            using var response1 = MockAsyncOperationHelper.AcceptedResponseWithOperationLocation(operationLocation);
            using var response2 = MockAsyncOperationHelper.SuccessResponseWithInProgressStatus();
            using var response3 = MockAsyncOperationHelper.SuccessResponseWithSucceededStatus(new TestResource());
            httpTest.ResponseQueue.Enqueue(response1);
            httpTest.ResponseQueue.Enqueue(response2);
            httpTest.ResponseQueue.Enqueue(response3);

            var additionalHeader = new Dictionary<string, string>
            {
                { "key", "value" },
            };

            var response = await _marketplaceRestClient.SendRequestWithPollingAsync<TestResource>(
                HttpMethod.Put,
                "/path/test",
                content: "somecontent",
                additionalHeaders: additionalHeader);

            httpTest.ShouldHaveCalled(operationLocation)
                .WithVerb(HttpMethod.Get)
                .WithHeader("key", "value")
                .Times(2);
        }

        [Fact]
        public async Task SendRequestWithPollingAsync_throws_exception_if_operation_location_is_not_set_Async()
        {
            using var httpTest = new HttpTest();
            using var response = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.Accepted };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(new TimeSpan(0, 0, 1));
            httpTest.ResponseQueue.Enqueue(response);

            await Assert.ThrowsAsync<MarketplaceException>(async () => await _marketplaceRestClient.SendRequestWithPollingAsync<TestResource>(HttpMethod.Put, "/path/test", content: "somecontent"));
        }

        [Fact]
        public async Task SendRequestWithPollingAsync_throws_exception_if_retry_value_is_not_set_Async()
        {
            using var httpTest = new HttpTest();
            using var response = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.Accepted };
            response.Headers.Add("Operation-Location", "https://mockoperationlocation.com");
            httpTest.ResponseQueue.Enqueue(response);

            await Assert.ThrowsAsync<MarketplaceException>(async () => await _marketplaceRestClient.SendRequestWithPollingAsync<TestResource>(HttpMethod.Put, "/path/test", content: "somecontent"));
        }

        internal class TestResource : MarketplaceAsyncOperationResponse
        {
        }
    }
}
