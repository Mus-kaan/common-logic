//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.TokenManager;
using System;

namespace Microsoft.Liftr.RPaaS.Hosting
{
    public class MetaRPOptions
    {
        public string MetaRPEndpoint { get; set; }

        public Uri KeyVaultEndpoint { get; set; }

        public string AccessorClientId { get; set; }

        public string AccessorCertificateName { get; set; }

        public TokenManagerConfiguration TokenManagerConfiguration { get; set; }
    }
}
