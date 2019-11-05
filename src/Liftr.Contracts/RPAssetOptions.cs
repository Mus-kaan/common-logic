//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr
{
    public sealed class RPAssetOptions
    {
        public string CosmosConnectionString { get; set; }

        public string StorageConnectionString { get; set; }

        public string DataPlaneStorageConnectionStrings { get; set; }

        public string DataPlaneSubscriptions { get; set; }
    }
}
