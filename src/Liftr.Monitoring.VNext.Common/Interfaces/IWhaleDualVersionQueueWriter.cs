//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.Common.Interfaces
{
    public interface IWhaleDualVersionQueueWriter
    {
        Task AddMessageAsync(string subscriptionId, string message, CancellationToken cancellationToken = default);
    }
}