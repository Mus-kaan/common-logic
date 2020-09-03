//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class SaasResourcesListResponse
    {
        /// <summary>
        /// List of the Datadog Saas resources.
        /// </summary>
        [JsonProperty("subscriptions")]
        public IEnumerable<MarketplaceSubscriptionDetailsEntity> Subscriptions { get; set; }

        /// <summary>
        /// Link to the next set of results, if any.
        /// </summary>
        [JsonProperty("nextLink")]
        public string NextLink { get; set; }
    }
}
