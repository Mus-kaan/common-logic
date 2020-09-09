//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    /// <summary>
    /// Request metadata for Liftr Billing API
    /// </summary>
    public class BillingRequestMetadata
    {
        [JsonProperty(PropertyName = "x-ms-requestid")]
        public string MSRequestId { get; set; }

        [JsonProperty(PropertyName = "x-ms-correlationid")]
        public string MSCorrelationId { get; set; }
    }
}
