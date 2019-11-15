//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    public class DataPlaneSubscriptionInfo
    {
        [JsonProperty("subId")]
        public string SubscriptionId { get; set; }

        [JsonProperty("storAccts")]
        public IEnumerable<string> StorageAccountIds { get; set; }
    }
}
