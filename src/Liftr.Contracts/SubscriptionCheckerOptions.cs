//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    public class SubscriptionCheckerOptions
    {
        /// <summary>
        /// The list of subscriptions.
        /// </summary>
        public IEnumerable<string> Subscriptions { get; set; }
    }
}
