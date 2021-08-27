﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Options;
using Microsoft.Liftr.Marketplace.TokenService.Options;

namespace Microsoft.Liftr.Marketplace.ARM.Options
{
    public class MarketplaceARMClientAuthOptions : TokenServiceAuthOptions
    {
        public MarketplaceAPIOptions API { get; set; } = null!;
    }
}
