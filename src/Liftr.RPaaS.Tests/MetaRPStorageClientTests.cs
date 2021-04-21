//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.ARM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.RPaaS.Tests
{
    public class MetaRPStorageClientTests
    {
        private readonly Serilog.ILogger _logger;

        public MetaRPStorageClientTests(ITestOutputHelper output)
        {
            _logger = TestLogger.GenerateLogger(output);
        }

        [Fact]
        public async Task Returns_all_resources_in_provider_namespace_Async()
        {
            using var handler = new MetaRPMessageHandler();
            var listResponse1 = new ListResponse<TestResource>()
            {
                Value = new List<TestResource>() { Constants.Resource1() },
                NextLink = Constants.NextLink,
            };

            using var response1 = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(listResponse1.ToJson()),
            };

            var listResponse2 = new ListResponse<TestResource>()
            {
                Value = new List<TestResource>() { Constants.Resource2() },
                NextLink = null,
            };

            using var response2 = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(listResponse2.ToJson()),
            };

            handler.QueueResponse(response1);
            handler.QueueResponse(response2);

            using var httpClient = new HttpClient(handler, false);
            var metaRpClient = new MetaRPStorageClient(
                httpClient,
                new MetaRPOptions()
                {
                    MetaRPEndpoint = new Uri(Constants.MetaRpEndpoint),
                    UserRPTenantId = "tenantId",
                },
                (_) => Task.FromResult("authToken"),
                _logger);

            var resources = await metaRpClient.ListResourcesAsync<TestResource>(Constants.RequestPath, Constants.ApiVersion);

            Assert.Equal(2, resources.Count());
            Assert.Equal(Constants.Resource1().Id, resources.ElementAt(0).Id);
            Assert.Equal(Constants.Resource2().Id, resources.ElementAt(1).Id);
        }

        [Fact]
        public async Task Returns_all_filtered_resources_in_provider_namespace_Async()
        {
            using var handler = new MetaRPMessageHandler();

            var listResponse3 = new ListResponse<TestResource>()
            {
                Value = new List<TestResource>() { Constants.Resource3() },
                NextLink = null,
            };

            using var response3 = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(listResponse3.ToJson()),
            };

            handler.QueueResponse(response3);

            using var httpClient = new HttpClient(handler, false);
            var metaRpClient = new MetaRPStorageClient(
                httpClient,
                new MetaRPOptions()
                {
                    MetaRPEndpoint = new Uri(Constants.MetaRpEndpoint),
                    UserRPTenantId = "tenantId",
                },
                (_) => Task.FromResult("authToken"),
                _logger);

            string filterCondition = "Properties.userDetail.emailAddress eq '{resource.Properties.UserDetail.EmailAddress}'";
            var resources = await metaRpClient.ListFilteredResourcesAsync<TestResource>(Constants.RequestPath, Constants.ApiVersion, filterCondition);

            Assert.Single(resources);
            Assert.Equal(Constants.Resource3().Id, resources.ElementAt(0).Id);
        }

        [Fact]
        public async Task Throws_arg_null_exception_filtered_resources_in_provider_namespace_Async()
        {
            using var handler = new MetaRPMessageHandler();

            var listResponse3 = new ListResponse<TestResource>()
            {
                Value = new List<TestResource>() { Constants.Resource3() },
                NextLink = null,
            };

            using var response3 = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(listResponse3.ToJson()),
            };

            handler.QueueResponse(response3);

            using var httpClient = new HttpClient(handler, false);
            var metaRpClient = new MetaRPStorageClient(
                httpClient,
                new MetaRPOptions()
                {
                    MetaRPEndpoint = new Uri(Constants.MetaRpEndpoint),
                    UserRPTenantId = "tenantId",
                },
                (_) => Task.FromResult("authToken"),
                _logger);

            string filterCondition = string.Empty;
            await Assert.ThrowsAsync<ArgumentNullException>(() => metaRpClient.ListFilteredResourcesAsync<TestResource>(Constants.RequestPath, Constants.ApiVersion, filterCondition));

            string filterCondition2 = null;
            await Assert.ThrowsAsync<ArgumentNullException>(() => metaRpClient.ListFilteredResourcesAsync<TestResource>(Constants.RequestPath, Constants.ApiVersion, filterCondition2));
        }

        [Fact]
        public async Task Retries_on_patch_if_resource_not_found_Async()
        {
            using var handlerMock = new MetaRPMessageHandler();

            using var response1 = new HttpResponseMessage() { StatusCode = HttpStatusCode.NotFound };
            using var response2 = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK };
            handlerMock.QueueResponse(response1);
            handlerMock.QueueResponse(response1);
            handlerMock.QueueResponse(response2);

            using var httpClient = new HttpClient(handlerMock, false);
            var metaRpClient = new MetaRPStorageClient(
                httpClient,
                new MetaRPOptions()
                {
                    MetaRPEndpoint = new Uri(Constants.MetaRpEndpoint),
                    UserRPTenantId = "tenantId",
                },
                (_) => Task.FromResult("authToken"),
                _logger);

            var response = await metaRpClient.PatchResourceAsync(new TestResource(), "testresourceid", "testtenantid", Constants.ApiVersion);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, handlerMock.SendCalled);
        }
    }
}
