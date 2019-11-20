//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class ComputeTestModel
    {
        public RegionalComputeOptions Options { get; set; }

        public AKSInfo AKS { get; set; }

        public CertificateOptions GenevaCert { get; set; }

        public CertificateOptions SSLCert { get; set; }

        public CertificateOptions FirstPartyCert { get; set; }
    }
}
