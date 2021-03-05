//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    /// <summary>
    /// Marketplace billing API Success response
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#responses
    /// </summary>
    public class MeteredBillingSuccessResponse : MeteredBillingRequestResponse
    {
        /// <summary>
        /// Unique identifier associated with the usage event
        /// </summary>
        [JsonProperty("usageEventId")]
        public Guid UsageEventId { get; set; }

        /// <summary>
        /// Request status i.e. Accepted|NotProcessed|Expired
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Time this message was created in UTC
        /// </summary>
        [JsonProperty("messageTime")]
        public DateTime MessageTime { get; set; }

        /// <summary>
        /// Identifier of the resource against which usage is emitted
        /// </summary>
        [JsonProperty("resourceId")]
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Quantity used
        /// </summary>
        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        /// <summary>
        /// Dimension identifier
        /// </summary>
        [JsonProperty("dimension")]
        public string Dimension { get; set; }

        /// <summary>
        /// Time in UTC when the usage event occurred
        /// </summary>
        [JsonProperty("effectiveStartTime")]
        public DateTime EffectiveStartTime { get; set; }

        /// <summary>
        /// Plan associated with the purchased offer
        /// </summary>
        [JsonProperty("planId")]
        public string PlanId { get; set; }
    }
}
