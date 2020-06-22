//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Marketplace
{
    public sealed class SaaSClientHackOptions
    {
        /// <summary>
        /// The list of subscriptions that we will ignore SaaS creation failures.
        /// </summary>
        public IEnumerable<string> IgnoringSubscriptions { get; set; }
    }
}
