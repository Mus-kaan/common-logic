//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Monitoring.VNext.Common
{
    public class WhaleSubscriptionOptions
    {
        public List<string> V2Subscriptions { get; set; }

        public bool IsWhaleV2 { get; set; }

        public bool IsV2Subscription(string subscriptionId)
        {
            return V2Subscriptions != null && V2Subscriptions.Contains(subscriptionId);
        }

        public bool ShouldProcessNotification(string subscriptionId)
        {
            var isV2Sub = IsV2Subscription(subscriptionId);
            return isV2Sub == IsWhaleV2;
        }
    }
}