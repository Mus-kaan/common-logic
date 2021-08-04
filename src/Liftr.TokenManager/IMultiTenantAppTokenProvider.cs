//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    public interface IMultiTenantAppTokenProvider : IDisposable
    {
        Task<string> GetTokenAsync(string tenantId, CancellationToken cancellationToken = default);
    }
}
