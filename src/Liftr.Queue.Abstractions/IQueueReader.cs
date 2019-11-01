//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Queue
{
    public interface IQueueReader
    {
        /// <summary>
        /// Start listening to a queue. This will not have order guarantee, i.e. no FIFO.
        /// </summary>
        Task StartListeningAsync(Func<LiftrQueueMessage, QueueMessageProcessingResult, CancellationToken, Task> messageProcessingCallback, CancellationToken cancellationToken = default(CancellationToken));
    }
}
