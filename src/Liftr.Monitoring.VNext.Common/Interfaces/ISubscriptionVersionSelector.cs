//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Liftr.Monitoring.VNext.Common.Interfaces
{
    public interface ISubscriptionVersionSelector
    {
         bool IsV2Subscription(string subscriptionId);
    }
}