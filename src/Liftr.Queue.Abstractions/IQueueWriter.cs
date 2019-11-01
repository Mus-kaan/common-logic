//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Queue
{
    public interface IQueueWriter
    {
        /// <summary>
        /// Add a message to the queue. This will not have order guarantee, i.e. no FIFO.
        /// </summary>
        Task AddMessageAsync(string message, CancellationToken cancellationToken = default(CancellationToken));
    }
}
