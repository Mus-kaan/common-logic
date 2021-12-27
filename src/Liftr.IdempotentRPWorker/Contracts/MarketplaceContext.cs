//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.Contracts.Marketplace;

namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    public class MarketplaceContext
    {
        public string PublisherId { get; set; }

        public string OfferId { get; set; }

        public string Name { get; set; }

        public string PlanId { get; set; }

        public string PaymentChannelType { get; set; }

        public string TermId { get; set; }

        public int? Quantity { get; set; }

        public bool? IsSubscriptionLevel { get; set; }

        public string TermUnit { get; set; }

        public BillingTermTypes TermType { get; set; }

        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        public bool IsValid()
        {
            return
                !string.IsNullOrEmpty(PublisherId) &&
                !string.IsNullOrEmpty(OfferId) &&
                !string.IsNullOrEmpty(Name) &&
                !string.IsNullOrEmpty(PlanId) &&
                !string.IsNullOrEmpty(TermUnit);
        }
    }
}
