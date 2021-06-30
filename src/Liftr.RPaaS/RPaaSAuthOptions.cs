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
        /// The authentication endpoint.
        /// </summary>
        public Uri Instance { get; set; }

        /// <summary>
        /// The tenant ID. Should be "common", since it's a multi-tenant scenario.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Our app id.
        /// </summary>
        public string ClientId { get; set; }
    }
}
