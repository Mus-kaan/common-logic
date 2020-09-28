//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.Whale;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Models;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Moq;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Liftr.Monitoring.Whale.Tests
{
    public class WhaleFilterClientTests
    {
        private readonly IAzureClientsProvider _clientProvider;
        private readonly ILogger _logger;

        public WhaleFilterClientTests()
        {
            var clientProviderMock = new Mock<IAzureClientsProvider>();
            var loggerMock = new Mock<ILogger>();

            clientProviderMock
                .Setup(c => c.GetResourceGraphClientAsync(It.IsAny<string>()))
#pragma warning disable CA2000 // Dispose objects before losing scope
                .ReturnsAsync(new ResourceGraphClientMock());
#pragma warning restore CA2000 // Dispose objects before losing scope

            _clientProvider = clientProviderMock.Object;
            _logger = loggerMock.Object;
        }

        [Fact]
        public void WhaleFilterClient_InvalidParameters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new WhaleFilterClient(null, _logger));
            Assert.Throws<ArgumentNullException>(() => new WhaleFilterClient(_clientProvider, null));
        }

        [Fact]
        public async Task ListResourcesByTagsAsync_WithOnlyInclusion_ExpectedBehaviorAsync()
        {
            var subscriptionId = "mySub";
            var tenantId = "tenantId";
            var tags = new List<FilteringTag>();
            var client = new WhaleFilterClient(_clientProvider, _logger);

            var result = await client.ListResourcesByTagsAsync(subscriptionId, tenantId, tags);

            // Ensure 2 resources were returned
            Assert.True(result.Count() == 2);

            // Ensure IDs match expected values
            Assert.Equal(
                "/subscriptions/mySub/resourceGroups/myRg/providers/My.Crosoft/resources/myResource",
                result.First().Id);
            Assert.Equal(
                "/subscriptions/mySub/resourceGroups/myRg/providers/My.Crosoft/resources/myResource2",
                result.Last().Id);

            // Ensure locations match expected values
            Assert.Equal("westus", result.First().Location);
            Assert.Equal("brazilsouth", result.Last().Location);
        }
    }

    internal class ResourceGraphClientMock : IResourceGraphClient
    {
        private readonly IEnumerable<MonitoredResource> _responsePage1;
        private readonly IEnumerable<MonitoredResource> _responsePage2;

        public ResourceGraphClientMock()
        {
            _responsePage1 = new List<MonitoredResource>()
            {
                new MonitoredResource()
                {
                    Id = "/subscriptions/mySub/resourceGroups/myRg/providers/My.Crosoft/resources/myResource",
                    Location = "westus",
                },
            };

            _responsePage2 = new List<MonitoredResource>()
            {
                new MonitoredResource()
                {
                    Id = "/subscriptions/mySub/resourceGroups/myRg/providers/My.Crosoft/resources/myResource2",
                    Location = "brazilsouth",
                },
            };
        }

        public Uri BaseUri { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public JsonSerializerSettings SerializationSettings => throw new NotImplementedException();

        public JsonSerializerSettings DeserializationSettings => throw new NotImplementedException();

        public ServiceClientCredentials Credentials => throw new NotImplementedException();

        public string ApiVersion => throw new NotImplementedException();

        public string AcceptLanguage { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int? LongRunningOperationRetryTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool? GenerateClientRequestId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IOperations Operations => throw new NotImplementedException();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<AzureOperationResponse<QueryResponse>> ResourcesWithHttpMessagesAsync(
            QueryRequest query,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            var queryResponse = new QueryResponse();

            if (query.Subscriptions.First() != "mySub" || query.Subscriptions.Count() != 1)
            {
                throw new ArgumentException("Invalid subscription");
            }

            if (string.IsNullOrEmpty(query.Options.SkipToken))
            {
                queryResponse.Data = _responsePage1;
                queryResponse.SkipToken = "skipToken";
            }
            else if (query.Options.SkipToken == "skipToken")
            {
                queryResponse.Data = _responsePage2;
            }
            else
            {
                throw new ArgumentException("Invalid skip token");
            }

            return new AzureOperationResponse<QueryResponse>() { Body = queryResponse };
        }

        internal void Dispose(bool disposing)
        {
        }
    }
}
