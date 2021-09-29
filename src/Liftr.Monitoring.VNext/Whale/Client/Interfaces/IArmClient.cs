//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.Whale.Client.Interfaces
{
    public interface IArmClient
    {
        Task<string> GetResourceAsync(string resourceId, string apiVersion, string tenantId);
        Task PutResourceAsync(string resourceId, string apiVersion, string resourceBody, string tenantId, CancellationToken cancellationToken = default);
        Task DeleteResourceAsync(string resourceId, string apiVersion, string tenantId, CancellationToken cancellationToken = default);
    }
}