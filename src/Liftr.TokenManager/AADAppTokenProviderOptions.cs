//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.TokenManager
{
    public class AADAppTokenProviderOptions
    {
        /// <summary>
        /// The AAD endpoint to be called.
        /// </summary>
        public string AadEndpoint { get; set; }

        /// <summary>
        /// The resource of the token.
        /// </summary>
        public string TargetResource { get; set; }

        /// <summary>
        /// Application Id (client Id).
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Key Vault Endpoint.
        /// </summary>
        public Uri KeyVaultEndpoint { get; set; }

        /// <summary>
        /// The name of the certificate in Key Vault.
        /// </summary>
        public string CertificateName { get; set; }
    }

    public class SingleTenantAADAppTokenProviderOptions : AADAppTokenProviderOptions
    {
        /// <summary>
        /// Tenant Id
        /// </summary>
        public string TenantId { get; set; }
    }
}
