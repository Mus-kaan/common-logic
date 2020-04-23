//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Provisioning;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class GlobalCallbackParameters : BaseCallbackParameters
    {
        public ProvisionedGlobalResources Resources { get; set; }
    }
}
