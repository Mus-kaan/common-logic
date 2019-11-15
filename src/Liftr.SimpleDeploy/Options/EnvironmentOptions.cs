//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class EnvironmentOptions : RunningEnvironmentOptions
    {
        public CertificateOptions GenevaCert { get; set; }

        public CertificateOptions FirstPartyCert { get; set; }

        public AKSInfo AKSInfo { get; set; }

        public LiftrAzureOptions LiftrAzureOptions { get; set; }
    }
}
