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
        }
    }
}
