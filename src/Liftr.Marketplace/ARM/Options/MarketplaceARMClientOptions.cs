//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Options;
using Microsoft.Liftr.TokenManager.Options;

namespace Microsoft.Liftr.Marketplace.ARM.Options
{
    public class MarketplaceARMClientOptions
    {
        public MarketplaceAPIOptions API { get; set; } = null!;

        public SingleTenantAADAppTokenProviderOptions MarketplaceFPAOptions { get; set; }
    }
}
