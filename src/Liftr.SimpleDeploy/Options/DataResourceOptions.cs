﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.SimpleDeploy
{
    public class DataResourceOptions : BaseResourceOptions
    {
        public string DataBaseName { get; set; }

        public bool CreateRegionalKeyVault { get; set; }

        public int DataPlaneStorageCount { get; set; }
    }
}
