//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.TokenManager
{
    public class TokenManagerConfiguration
    {
        public string TargetResource { get; set; }

        public string AadEndpoint { get; set; }

        public string TenantId { get; set; }
    }
}
