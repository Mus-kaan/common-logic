//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;
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
            using (var handler = new MockHttpMessageHandler())
            using (var httpClient = new HttpClient(handler, false))
            {
                var metaRpClient = new MetaRPStorageClient(
                    new Uri(Constants.MetaRpEndpoint),
                    httpClient,
                    new MetaRPOptions() { UserRPTenantId = "tenantId" },
                    (_) => Task.FromResult("authToken"),
                    _logger);

                var resources = await metaRpClient.ListResourcesAsync<TestResource>(Constants.RequestPath, Constants.ApiVersion);

                Assert.Equal(2, resources.Count());
                Assert.Equal(Constants.Resource1().Id, resources.ElementAt(0).Id);
                Assert.Equal(Constants.Resource2().Id, resources.ElementAt(1).Id);
            }
        }
    }
}
