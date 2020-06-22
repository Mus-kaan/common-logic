//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Marketplace.Saas.Models
{
    public class ActivateSubscriptionRequest
    {
        public ActivateSubscriptionRequest(MarketplaceSubscription marketplaceSubscription, string planId, int? quantity)
        {
            if (string.IsNullOrWhiteSpace(planId))
            {
                throw new ArgumentNullException(nameof(planId), "PlanId should not be empty");
            }

            MarketplaceSubscription = marketplaceSubscription ?? throw new ArgumentNullException(nameof(marketplaceSubscription));
            PlanId = planId;
            Quantity = quantity;
        }

        [JsonIgnore]
        public MarketplaceSubscription MarketplaceSubscription { get; }

        public string PlanId { get; }

        public int? Quantity { get; }
    }
}
