//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.Queue;
using System.Threading.Tasks;

namespace Microsoft.Liftr.IdempotentRPWorker.Interfaces
{
    public interface IWorkerCommandProcessor
    {
        Task<BaseResource> HandleMessageAsync(RPWorkerQueueMessage workerMessage, LiftrQueueMessage message = null);
    }
}
