//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    /// <summary>
    /// Marketplace billing API Conflict response
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#responses
    /// </summary>
    public class MeteredBillingConflictResponse : MeteredBillingRequestResponse
    {
        /// <summary>
        /// Message for conflict error
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Addition information for conflict error
        /// </summary>
        [JsonPropertyName("additionalInfo")]
        public AdditionalInfo AdditionalInfo { get; set; }
    }

    public class AdditionalInfo
    {
        /// <summary>
        /// Unique identifier associated with the usage event
        /// </summary>
        [JsonPropertyName("acceptedMessage")]
        public AcceptedMessage AcceptedMessage { get; set; }
    }

    public class AcceptedMessage
    {
        /// <summary>
        /// Unique identifier associated with the usage event
        /// </summary>
        [JsonPropertyName("usageEventId")]
        public Guid UsageEventId { get; set; }

        /// <summary>
        /// Request status i.e. Accepted|NotProcessed|Expired
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Time this message was created in UTC
        /// </summary>
        [JsonPropertyName("messageTime")]
        public DateTime MessageTime { get; set; }

        /// <summary>
        /// Identifier of the resource against which usage is emitted
        /// </summary>
        [JsonPropertyName("resourceId")]
        public string ResourceId { get; set; }

        /// <summary>
        /// Quantity used
        /// </summary>
        [JsonPropertyName("quantity")]
        public double Quantity { get; set; }

        /// <summary>
        /// Dimension identifier
        /// </summary>
        [JsonPropertyName("dimension")]
        public string Dimension { get; set; }

        /// <summary>
        /// Time in UTC when the usage event occurred
        /// </summary>
        [JsonPropertyName("effectiveStartTime")]
        public DateTime EffectiveStartTime { get; set; }

        /// <summary>
        /// Plan associated with the purchased offer
        /// </summary>
        [JsonPropertyName("planId")]
        public string PlanId { get; set; }
    }
}
