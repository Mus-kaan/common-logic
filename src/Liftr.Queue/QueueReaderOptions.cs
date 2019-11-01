//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Queue
{
    public class QueueReaderOptions
    {
        public int MaxConcurrentCalls { get; set; }

        public void CheckValues()
        {
            if (MaxConcurrentCalls < 1 || MaxConcurrentCalls > 50)
            {
                throw new InvalidOperationException($"nameof(MaxConcurrentCalls) should be within [1,50]");
            }
        }
    }
}
