//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class PrepareAKSCallbackParameters : BaseCallbackParameters
    {
        public ActionExcutorRegionOptions RegionOptions { get; set; }

        public IPublicIPAddress AKSInboundIP { get; set; }
    }
}
