//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.TokenManager.Options;
using System;

namespace Microsoft.Liftr.RPaaS
{
    public class MetaRPOptions
    {
        public Uri MetaRPEndpoint { get; set; }

        public string UserRPTenantId { get; set; }

        public AADAppTokenProviderOptions FPAOptions { get; set; }
    }
}
