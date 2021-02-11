//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    public class DataAssetOptions
    {
        [JsonProperty("dbConn")]
        public string RegionalDBConnectionString { get; set; }

        [JsonProperty("dbROConn")]
        public string RegionalDBReadonlyConnectionString { get; set; }

        [JsonProperty("gbldbConn")]
        public string GlobalDBConnectionString { get; set; }

        [JsonProperty("gbldbROConn")]
        public string GlobalDBReadonlyConnectionString { get; set; }

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
    }
}
