//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Marketplace.Agreement.Models
{
    public class AgreementResponseProperties
    {
        public string Publisher { get; set; }

        public string Product { get; set; }

        public string Plan { get; set; }

        public string LicenseTextLink { get; set; }

        public string PrivacyPolicyLink { get; set; }

        public string MarketplaceTermsLink { get; set; }

        public DateTime RetrieveDatetime { get; set; }

        public string Signature { get; set; }

        public bool Accepted { get; set; }
    }
}
