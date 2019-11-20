//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class DataResourceOptions : BaseResourceOptions
    {
        public string DataBaseName { get; set; }

        public string HostName { get; set; }

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }
    }
}
