//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Contracts
{
    public class RPAssetOptions
    {
        [JsonProperty("dbConns")]
        public IEnumerable<CosmosDBConnectionString> CosmosDBConnectionStrings { get; set; }

        [JsonProperty("gblDbConns")]
        public IEnumerable<CosmosDBConnectionString> GlobalCosmosDBConnectionStrings { get; set; }

        [JsonProperty("activeKey")]
        public string ActiveKeyName { get; set; }

        [JsonProperty("storName")]
        public string StorageAccountName { get; set; }

        [JsonProperty("gblStorName")]
        public string GlobalStorageAccountName { get; set; }

        [JsonProperty("acisStorName")]
        public string ACISStorageAccountName { get; set; }

        [JsonProperty("acisStorConn")]
        public string ACISStorageConnectionString { get; set; }

        [JsonProperty("dpSubs")]
        public IEnumerable<DataPlaneSubscriptionInfo> DataPlaneSubscriptions { get; set; }

        public string GetActiveCosmosDBConnectionString()
        {
            return CosmosDBConnectionStrings
                .Where(cs => cs.Description.OrdinalEquals(ActiveKeyName))
                .FirstOrDefault()
                ?.ConnectionString;
        }

        public string GetActiveGlobalCosmosDBConnectionString()
        {
            return GlobalCosmosDBConnectionStrings
                .Where(cs => cs.Description.OrdinalEquals(ActiveKeyName))
                .FirstOrDefault()
                ?.ConnectionString;
        }
    }
}
