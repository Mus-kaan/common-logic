//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Queue
{
    public class QueueReaderOptions
    {
        public int MaxConcurrentCalls { get; set; }

        public int MaxDequeueCount { get; set; } = 7;

        public long VisibilityTimeoutInSeconds { get; set; } = (int)QueueParameters.VisibilityTimeout.TotalSeconds;

        public long MessageLeaseRenewIntervalInSeconds { get; set; } = (int)QueueParameters.MessageLeaseRenewInterval.TotalSeconds;

        public void CheckValues()
        {
            if (MaxConcurrentCalls < 1 || MaxConcurrentCalls > 50)
            {
                throw new InvalidOperationException($"{nameof(MaxConcurrentCalls)} should be within [1,50]");
            }

            if (MaxDequeueCount < 3 || MaxDequeueCount > 50)
            {
                throw new InvalidOperationException($"{nameof(MaxDequeueCount)} should be within [3,50]");
            }

            if (VisibilityTimeoutInSeconds < (int)QueueParameters.VisibilityTimeout.TotalSeconds || VisibilityTimeoutInSeconds > 7200)
            {
                throw new InvalidOperationException($"{nameof(VisibilityTimeoutInSeconds)} should be [{QueueParameters.VisibilityTimeout.TotalSeconds} , 7200]");
            }

            if (MessageLeaseRenewIntervalInSeconds * 3 > VisibilityTimeoutInSeconds)
            {
                throw new InvalidOperationException($"{nameof(MessageLeaseRenewIntervalInSeconds)} should be at least three times more than visibility time out");
            }
        }
    }
}
