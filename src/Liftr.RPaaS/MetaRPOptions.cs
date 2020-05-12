//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.TokenManager.Options;

namespace Microsoft.Liftr.RPaaS
{
    public class MetaRPOptions
    {
        public string MetaRPEndpoint { get; set; }

        public string UserRPTenantId { get; set; }

        public AADAppTokenProviderOptions FPAOptions { get; set; }
    }
}
