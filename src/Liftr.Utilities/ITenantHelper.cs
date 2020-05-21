//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.Utilities
{
    public interface ITenantHelper
    {
        Task<string> GetTenantIdForSubscriptionAsync(string subscriptionId);
    }
}
