//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Newtonsoft.Json;

namespace Microsoft.Liftr.Marketplace.Saas.Contracts
{
    public class ResolvedMarketplaceSubscription
    {
        public string OfferId { get; set; }

        public string PlanId { get; set; }

        public int? Quantity { get; set; }

        [JsonProperty("id")]
        [JsonConverter(typeof(MarketplaceSubscriptionConverter))]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        public string SubscriptionName { get; set; }
    }
}
