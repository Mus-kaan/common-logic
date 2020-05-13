//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.TokenManager.Options
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

        public void CheckValues()
        {
            if (KeyVaultEndpoint == null)
            {
                throw new InvalidOperationException($"'{nameof(KeyVaultEndpoint)}' is null. Please make sure '{nameof(AADAppTokenProviderOptions)}.{nameof(KeyVaultEndpoint)}' is configured correctly.");
            }

            if (string.IsNullOrEmpty(CertificateName))
            {
                throw new InvalidOperationException($"'{nameof(CertificateName)}' is null. Please make sure '{nameof(AADAppTokenProviderOptions)}.{nameof(CertificateName)}' is configured correctly.");
            }

            if (string.IsNullOrEmpty(AadEndpoint))
            {
                throw new InvalidOperationException($"'{nameof(AadEndpoint)}' is null. Please make sure '{nameof(AADAppTokenProviderOptions)}.{nameof(AadEndpoint)}' is configured correctly.");
            }

            if (string.IsNullOrEmpty(ApplicationId))
            {
                throw new InvalidOperationException($"'{nameof(ApplicationId)}' is null. Please make sure '{nameof(AADAppTokenProviderOptions)}.{nameof(ApplicationId)}' is configured correctly.");
            }
        }
    }

    public class SingleTenantAADAppTokenProviderOptions : AADAppTokenProviderOptions
    {
        /// <summary>
        /// Tenant Id
        /// </summary>
        public string TenantId { get; set; }
    }
}
