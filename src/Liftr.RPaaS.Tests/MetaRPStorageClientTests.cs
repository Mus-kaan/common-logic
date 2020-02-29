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
        [Fact]
        public async Task Returns_all_resources_in_provider_namespace_Async()
        {
            using var httpClient = new HttpClient(new MockHttpMessageHandler(), false);

            var metaRpClient = new MetaRPStorageClient(Constants.MetaRpEndpoint, httpClient, () => Task.FromResult("authToken"));
            var resources = await metaRpClient.ListResourcesAsync<TestResource>(Constants.RequestPath, Constants.ApiVersion);

            Assert.Equal(2, resources.Count());
            Assert.Equal(Constants.Resource1().Id, resources.ElementAt(0).Id);
            Assert.Equal(Constants.Resource2().Id, resources.ElementAt(1).Id);
        }
    }
}
