//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    public class WorkerResults
    {
        public BaseResource UpdatedResource { get; set; }

        public StateContext UpdatedStateContext { get; set; }
    }
}
