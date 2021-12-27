//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    /// <summary>
    /// Commands that can be processed by the RP worker.
    /// </summary>
    public enum RPWorkerQueueCommandEnum
    {
        CreateResource,

        DeleteResource,

        NotifyPartner,

        UpdateResource,

        ConfigureSSO,

        CustomCommand,
    }
}
