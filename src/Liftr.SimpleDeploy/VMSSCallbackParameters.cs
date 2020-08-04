//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Provisioning;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class VMSSCallbackParameters : BaseCallbackParameters
    {
        public RegionalComputeOptions ComputeOptions { get; set; }

        public ProvisionedVMSSResources Resources { get; set; }
    }
}
