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
        public WebhookAction Action { get; set; }

        public Guid ActivityId { get; set; }

        public string OfferId { get; set; }

        // Operation Id is presented as Id property on the json payload
        [JsonProperty("Id")]
        public Guid OperationId { get; set; }

        public string PlanId { get; set; }

        public string PublisherId { get; set; }

        public int Quantity { get; set; }

        public OperationStatus Status { get; set; }

        [JsonProperty("subscriptionId")]
        [JsonConverter(typeof(MarketplaceSubscriptionConverter))]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        public DateTimeOffset TimeStamp { get; set; }

        public string RawPayload { get; set; }
    }
}
