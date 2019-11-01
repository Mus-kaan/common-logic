//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr
{
    public sealed class DataStorageOptions
    {
        public string CosmosConnectionString { get; set; }

        public string StorageConnectionString { get; set; }

        public string DataPlaneStorageConnectionStrings { get; set; }
    }
}
