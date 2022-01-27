//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DBService.Contracts
{
    public enum SaasSubscriptionStatus
    {
        Started = 0,
        PendingFulfillmentStart = 1,
        InProgress = 2,
        Subscribed = 3,
        Suspended = 4,
        Reinstated = 5,
        Succeeded = 6,
        Failed = 7,
        Unsubscribed = 8,
        Updating = 9,
        PlanChanged = 10,
        PlanRenew = 11,
    }
}
