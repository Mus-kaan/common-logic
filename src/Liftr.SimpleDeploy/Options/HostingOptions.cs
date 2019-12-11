//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class HostingOptions
    {
        public IEnumerable<HostingEnvironmentOptions> Environments { get; set; }
    }
}
