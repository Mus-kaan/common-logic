//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Logging.Contracts
{
    public interface ITimedOperationMetricsProcessor
    {
        void Process(ITimedOperation timedOperation);
    }
}
