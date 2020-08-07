//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Provisioning;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class RegionalDataCallbackParameters : BaseCallbackParameters
    {
        public RegionalDataOptions DataOptions { get; set; }

        public RegionOptions RegionOptions { get; set; }

        public ProvisionedRegionalDataResources Resources { get; set; }
    }
}
