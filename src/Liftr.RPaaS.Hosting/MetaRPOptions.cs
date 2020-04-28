//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.TokenManager;

namespace Microsoft.Liftr.RPaaS.Hosting
{
    public class MetaRPOptions
    {
        public string MetaRPEndpoint { get; set; }

        public SingleTenantAADAppTokenProviderOptions FPAOptions { get; set; }
    }
}
