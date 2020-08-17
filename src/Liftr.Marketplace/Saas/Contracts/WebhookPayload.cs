//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Liftr.Marketplace.Saas.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WebhookAction
    {
        // When the resource has been deleted
        Unsubscribe,

        // When the change plan operation has completed
        ChangePlan,

        // When the change quantity operation has completed
        ChangeQuantity,

        // When resource has been suspended
        Suspend,

        // When resource has been reinstated after suspension
        Reinstate,
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#implementing-a-webhook-on-the-saas-service
    /// </summary>
    public class WebhookPayload
    {
        [JsonProperty("action")]
        public WebhookAction Action { get; set; }

        [JsonProperty("activityId")]
        public Guid ActivityId { get; set; }

        [JsonProperty("offerId")]
        public string OfferId { get; set; }

        // Operation Id is presented as Id property on the json payload
        [JsonProperty("id")]
        public Guid OperationId { get; set; }

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("publisherId")]
        public string PublisherId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("status")]
        public OperationStatus Status { get; set; }

        [JsonProperty("subscriptionId")]
        [JsonConverter(typeof(MarketplaceSubscriptionConverter))]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        [JsonProperty("timeStamp")]
        public DateTimeOffset TimeStamp { get; set; }
    }
}
