//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Queue;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.IdempotentRPWorker.Interfaces
{
    public interface IRPWorkerOrchestration
    {
        Task ProcessQueueMessageAsync(LiftrQueueMessage message, QueueMessageProcessingResult result, CancellationToken cancellationToken);
    }
}
