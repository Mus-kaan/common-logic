//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.RPaaS
{
    public class RPaaSConfiguration
    {
        public string MetaRPEndpoint { get; set; }

        public string MetaRPAccessorClientId { get; set; }

        public string MetaRPAccessorVaultEndpoint { get; set; }

        public string MetaRPAccessorCertificateName { get; set; }
    }
}
