//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using System;

namespace Microsoft.Liftr.Marketplace.Agreement.Options
{
    public class MarketplaceAgreementAuthOptions
    {
        /// <summary>
        /// Key Vault Endpoint.
        /// </summary>
        public Uri KeyVaultEndpoint { get; set; }

        /// <summary>
        /// The name of the certificate in Key Vault.
        /// </summary>
        public string CertificateName { get; set; }
    }
}
