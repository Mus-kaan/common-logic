//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Whale
{
    public class WhaleFilterClient : IWhaleFilterClient
    {
        private readonly IAzureClientsProvider _clientProvider;
        private readonly ILogger _logger;

        public WhaleFilterClient(IAzureClientsProvider clientProvider, ILogger logger)
        {
            _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<MonitoredResource>> ListResourcesByTagsAsync(
            string subscriptionId, string tenantId, IEnumerable<FilteringTag> filteringTags)
        {
            using (var operation = _logger.StartTimedOperation(nameof(ListResourcesByTagsAsync)))
            {
                operation.SetContextProperty(nameof(subscriptionId), subscriptionId);
                operation.SetContextProperty(nameof(tenantId), tenantId);
                operation.SetContextProperty(nameof(filteringTags), filteringTags.ToJson());

                var resourceGraphClient = await _clientProvider.GetResourceGraphClientAsync(tenantId);
                var query = KustoQueryBuilder.GetQueryRequest(subscriptionId, filteringTags);
                var resourcesWithCorrectTag = new List<MonitoredResource>();

                do
                {
                    var queryResponse = await resourceGraphClient.ResourcesWithHttpMessagesAsync(query);

                    var idsInResponse = JsonConvert.DeserializeObject<IEnumerable<MonitoredResource>>(queryResponse.Body.Data.ToJson());
                    resourcesWithCorrectTag.AddRange(idsInResponse);

                    query.Options.SkipToken = queryResponse.Body.SkipToken;
                }
                while (!string.IsNullOrEmpty(query.Options.SkipToken));

                _logger.Information("Listed resources to be monitored from ARG. resourceCount: {resourceCount}, resourceList:{@resourceList}", resourcesWithCorrectTag.Count, resourcesWithCorrectTag);
                operation.SetProperty(nameof(resourcesWithCorrectTag) + "Count", resourcesWithCorrectTag.Count);

                return resourcesWithCorrectTag;
            }
        }
    }
}
