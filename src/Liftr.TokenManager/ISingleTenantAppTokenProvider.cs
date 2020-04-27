//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    public interface ISingleTenantAppTokenProvider : IDisposable
    {
        Task<string> GetTokenAsync();
    }
}
