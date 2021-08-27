//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Options;
using Microsoft.Liftr.TokenManager.Options;

namespace Microsoft.Liftr.Marketplace.TokenService.Options
{
    public class TokenServiceAuthOptions
    {
        public TokenServiceAPIOptions TokenServiceAPI { get; set; } = null!;

        public SingleTenantAADAppTokenProviderOptions AuthOptions { get; set; }
    }
}
