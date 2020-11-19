//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource
{
    public interface IAgreementResourceDataSource
    {
        Task AcceptAsync(string subscriptionId);

        Task<bool> GetAsync(string subscriptionId);
    }
}