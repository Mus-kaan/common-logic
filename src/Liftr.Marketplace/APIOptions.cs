//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Liftr.Marketplace
{
    public class APIOptions
    {
        [Required]
        public string ApiVersion { get; set; } = null!;

        [Required]
        public Uri Endpoint { get; set; } = null!;
    }
}
