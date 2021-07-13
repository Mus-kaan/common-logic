//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.Marketplace.ARM.Contracts
{
    public class MigrationRequest
    {
        /// <summary>
        /// Gets or sets the Saas Subscription Id
        /// </summary>
        [JsonProperty("saasSubscriptionId")]
        public string SaasSubscriptionId { get; set; }
    }
}
