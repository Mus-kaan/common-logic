//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Liftr.Marketplace.Options
{
    /// <summary>
    /// The marketplace endpoint for the creation, fulfillment and billing
    /// </summary>
    public class MarketplaceAPIOptions
    {
        [Required]
        public string ApiVersion { get; set; } = null!;

        [Required]
        public Uri Endpoint { get; set; } = null!;
    }
}
