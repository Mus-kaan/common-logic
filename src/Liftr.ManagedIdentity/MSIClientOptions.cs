//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.ManagedIdentity
{
    public class MSIClientOptions
    {
        /// <summary>
        /// Application Id (client Id).
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// The name of the certificate in Key Vault.
        /// </summary>
        public string CertificateName { get; set; }

        /// <summary>
        /// Key Vault Endpoint.
        /// </summary>
        public Uri KeyVaultEndpoint { get; set; }

        public void CheckValues()
        {
            if (KeyVaultEndpoint == null)
            {
                throw new InvalidOperationException($"'{nameof(KeyVaultEndpoint)}' is null. Please make sure '{nameof(MSIClientOptions)}.{nameof(KeyVaultEndpoint)}' is configured correctly.");
            }

            if (string.IsNullOrEmpty(CertificateName))
            {
                throw new InvalidOperationException($"'{nameof(CertificateName)}' is null. Please make sure '{nameof(MSIClientOptions)}.{nameof(CertificateName)}' is configured correctly.");
            }

            if (string.IsNullOrEmpty(ApplicationId))
            {
                throw new InvalidOperationException($"'{nameof(ApplicationId)}' is null. Please make sure '{nameof(MSIClientOptions)}.{nameof(ApplicationId)}' is configured correctly.");
            }
        }
    }
}
