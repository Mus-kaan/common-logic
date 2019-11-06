//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class EnvironmentOptions
    {
        public string TenantId { get; set; }

        public string ProvisioningRunnerClientId { get; set; }

        public CertificateOptions GenevaCert { get; set; }

        public CertificateOptions FirstPartyCert { get; set; }

        public AKSInfo AKSInfo { get; set; }
    }
}
