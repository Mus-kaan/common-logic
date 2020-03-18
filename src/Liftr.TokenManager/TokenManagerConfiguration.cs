//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.TokenManager
{
    public class TokenManagerConfiguration
    {
        /// <summary>
        /// The resource of the token.
        /// </summary>
        public string TargetResource { get; set; }

        /// <summary>
        /// The AAD endpoint to be called.
        /// </summary>
        public string AadEndpoint { get; set; }

        /// <summary>
        /// The default tenant id to be used by the token.
        /// </summary>
        public string TenantId { get; set; }
    }
}
