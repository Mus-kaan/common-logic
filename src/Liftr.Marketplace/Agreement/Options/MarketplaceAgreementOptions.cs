//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.Marketplace.Options;

namespace Microsoft.Liftr.Marketplace.Agreement.Options
{
    public class MarketplaceAgreementOptions
    {
        public MarketplaceAPIOptions API { get; set; } = null!;

        public MarketplaceAgreementAuthOptions AuthOptions { get; set; }
    }
}
