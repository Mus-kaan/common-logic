//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr
{
    public class RPAssetOptions
    {
        public string CosmosDBConnectionString { get; set; }

        public string StorageAccountName { get; set; }

        public IEnumerable<string> DataPlaneStorageAccounts { get; set; }

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }
    }
}
