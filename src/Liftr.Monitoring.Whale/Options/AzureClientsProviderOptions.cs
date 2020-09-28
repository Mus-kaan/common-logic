//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.TokenManager;
using System;

namespace Microsoft.Liftr.Monitoring.Whale.Options
{
    public class AzureClientsProviderOptions
    {
        /// <summary>
        /// The client id to authenticate whale requests.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The name of the certificate used for authenticating the client id.
        /// </summary>
        public string CertificateName { get; set; }

        /// <summary>
        /// The key vault which stores the certificate.
        /// </summary>
        public Uri KeyVaultEndpoint { get; set; }

        /// <summary>
        /// The ARM endpoint to be used.
        /// </summary>
        public Uri ArmEndpoint { get; set; }

        /// <summary>
        /// The token manager configuration used for acquiring bearer tokens.
        /// </summary>
        public TokenManagerConfiguration TokenManagerConfiguration { get; set; }
    }
}
