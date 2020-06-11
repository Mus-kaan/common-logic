//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Contracts.Marketplace
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BillingTermTypes
    {
        Monthly,
        Yearly,
    }

    public interface IMarketplaceSaasResource
    {
        string Name { get; set; }

        MarketplaceSubscription MarketplaceSubscription { get; set; }

        string Plan { get; set; }

        string BillingTermId { get; set; }

        BillingTermTypes BillingTermType { get; set; }

        int? Quantity { get; set; }
    }
}