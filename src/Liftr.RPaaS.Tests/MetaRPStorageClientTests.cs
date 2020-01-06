//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.ARM;
using Microsoft.Liftr.RPaaS;
using Microsoft.Liftr.Utilities;
using Moq;
using Moq.Protected;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.RPaaS.Tests
{
    public class MetaRPStorageClientTests
    {
        private const string providerNamespace = "Microsoft.Nginx";
        private const string resourceType = "frontend";
        private const string apiVersion = "2019-11-01-preview";
        private const string metaRpEndpoint = "https://metarp.com";

        [Fact]
        public async Task Returns_all_resources_in_provider_namespace_Async()
        {
            var resource1 = new TestResource()
            {
                Type = "Microsoft.Nginx/frontends",
                Id = "/subscriptions/f9aed45d-b9e6-462a-a3f5-6ab34857bc17/resourceGroups/myrg/providers/Microsoft.Nginx/frontends/frontend",
                Name = "frontend",
                Location = "eastus",
            };

            var resource2 = new TestResource()
            {
                Type = "Microsoft.Nginx/frontends",
                Id = "/subscriptions/f9aed45d-b9e6-462a-a3f5-6ab34857bc17/resourceGroups/myrg/providers/Microsoft.Nginx/frontends/frontend2",
                Name = "frontend2",
                Location = "eastus",
            };

            using var responseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent($@"{{ ""value"" : {new List<TestResource>() { resource1, resource2 }.ToJson()} }}"),
            };

            var requestMethod = HttpMethod.Get;
            var userRpSubscriptionId = Guid.NewGuid();
            var requestUri = new Uri($"{metaRpEndpoint}/subscriptions/{userRpSubscriptionId.ToString()}/providers/{providerNamespace}/{resourceType}?api-version={apiVersion}&$expand=crossPartitionQuery");
            using var httpclient = new HttpClient(GetMockHttpClient(requestMethod, requestUri, responseMessage).Object, false);

            var metaRpClient = new MetaRPStorageClient(metaRpEndpoint, httpclient, () => Task.FromResult("authToken"));
            var resources = await metaRpClient.ListAllResourcesOfTypeAsync<TestResource>(userRpSubscriptionId, providerNamespace, resourceType, apiVersion);

            Assert.Equal(2, resources.Count());
            Assert.Equal(resource1.Id, resources.ElementAt(0).Id);
            Assert.Equal(resource2.Id, resources.ElementAt(1).Id);
        }

        private Mock<HttpMessageHandler> GetMockHttpClient(HttpMethod requestMethod, Uri requestUri, HttpResponseMessage responseMessage)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Default);
            var httpRequestMessage = ItExpr.Is<HttpRequestMessage>(req => req.Method == requestMethod && req.RequestUri == requestUri);

            handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync", httpRequestMessage, ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(responseMessage)
           .Verifiable();

            return handlerMock;
        }
    }

    internal class TestResource : ARMResource
    {
        public override string Type { get; set; }
    }
}
