//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Options;
using Microsoft.Liftr.Marketplace.TokenService.Options;

namespace Microsoft.Liftr.Marketplace.Agreement.Options
{
    public class MarketplaceAgreementClientAuthOptions : TokenServiceAuthOptions
    {
        public MarketplaceAPIOptions API { get; set; } = null!;
    }
}
