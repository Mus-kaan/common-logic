//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Utilities;

namespace Microsoft.Liftr.Marketplace
{
    [SwaggerExtension(ExcludeFromSwagger = true)]
    public class MarketplaceSaasResource : IMarketplaceSaasResource
    {
        public MarketplaceSaasResource(
            MarketplaceSubscription marketplaceSubscription,
            string name,
            string plan,
            string billingTermId,
            BillingTermTypes billingTermType,
            int? quantity = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new System.ArgumentException("Name cannot be empty", nameof(name));
            }

            if (string.IsNullOrEmpty(plan))
            {
                throw new System.ArgumentException("Name cannot be empty", nameof(plan));
            }

            if (string.IsNullOrEmpty(billingTermId))
            {
                throw new System.ArgumentException("Name cannot be empty", nameof(billingTermId));
            }

            MarketplaceSubscription = marketplaceSubscription ?? throw new System.ArgumentNullException(nameof(marketplaceSubscription));
            PlanId = plan;
            BillingTermId = billingTermId;
            BillingTermType = billingTermType;
            Quantity = quantity ?? throw new System.ArgumentNullException(nameof(quantity));
            Name = name;
        }

        public string Name { get; set; }

        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        public string PlanId { get; set; }

        public int? Quantity { get; set; }

        public string BillingTermId { get; set; }

        public BillingTermTypes BillingTermType { get; set; }
    }
}
