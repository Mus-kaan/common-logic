//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Contracts;
using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Marketplace.Saas.Contracts
{
    /// <summary>
    /// Operation used by the Marketplace and RP to communicate on the updates made to the subscription
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#get-operation-status
    /// </summary>
    public class SubscriptionOperation : MarketplaceAsyncOperationResponse
    {
        public string Action { get; set; }

        public Guid ActivityId { get; set; }

        public Guid Id { get; set; }

        public string OfferId { get; set; }

        public string OperationRequestSource { get; set; }

        public string PlanId { get; set; }

        public string PublisherId { get; set; }

        public string Quantity { get; set; }

        public Uri ResourceLocation { get; set; }

        [JsonProperty("subscriptionId")]
        [JsonConverter(typeof(MarketplaceSubscriptionConverter))]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
