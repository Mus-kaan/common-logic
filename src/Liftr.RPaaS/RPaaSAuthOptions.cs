//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.RPaaS
{
    /// <summary>
    /// Options for authenticating RPaaS requests to our RP.
    /// </summary>
    public class RPaaSAuthOptions
    {
        /// <summary>
        /// The app id RPaaS will use.
        /// </summary>
        public string RPaaSAppId { get; set; }

        /// <summary>
        /// The audience of the token issued by RPaaS. Should be our app id.
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Whether RPaaS Auth process should be skipped (e.g. in dev environment)
        /// </summary>
        public bool ShouldSkip { get; set; } = false;
    }
}
