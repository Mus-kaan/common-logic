//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.RPaaS.Hosting
{
    public class MetaRPOptions
    {
        public string MetaRPEndpoint { get; set; }

        public string KeyVaultEndpoint { get; set; }

        public string AccessorClientId { get; set; }

        public string AccessorCertificateName { get; set; }
    }
}
