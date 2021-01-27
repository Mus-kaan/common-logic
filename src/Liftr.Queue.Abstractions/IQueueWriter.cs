//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Queue
{
    public interface IQueueWriter
    {
        /// <summary>
        /// Add a message to the queue. This will not have order guarantee, i.e. no FIFO.
        /// </summary>
        /// <param name="message">The message content</param>
        /// <param name="messageVisibilityTimeout">Message visibility timeout, which will make the message invisible until the visibility timeout expires.</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        Task AddMessageAsync(string message, TimeSpan? messageVisibilityTimeout = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
