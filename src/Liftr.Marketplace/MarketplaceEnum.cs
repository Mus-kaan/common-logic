//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Marketplace
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MarketplaceEnum
    {
        ListSubscriptions,
        ResolveToken,
        ActivateSubscription,
        UpdateOperation,
        GetOperation,
        ListOperations,
        ChangePlan,
        ChangeQuantity,
        DeleteSubscription,
        SuspendSubscription,
        ReinstateSubscription,
        BillingUsageEvent,
        BillingBatchUsageEvent,
        GetSubscription,
    }
}
