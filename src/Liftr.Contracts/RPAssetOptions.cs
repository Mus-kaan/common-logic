//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    public class RPAssetOptions
    {
        [JsonProperty("dbConns")]
        public IEnumerable<CosmosDBConnectionString> CosmosDBConnectionStrings { get; set; }

        [JsonProperty("activeKey")]
        public string ActiveKeyName { get; set; }

        [JsonProperty("storName")]
        public string StorageAccountName { get; set; }

        [JsonProperty("dpSubs")]
        public IEnumerable<DataPlaneSubscriptionInfo> DataPlaneSubscriptions { get; set; }
    }
}
