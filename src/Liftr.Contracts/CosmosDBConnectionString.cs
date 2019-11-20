//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.Contracts
{
    public class CosmosDBConnectionString
    {
        [JsonProperty(PropertyName = "val")]
        public string ConnectionString { get; set; }

        [JsonProperty(PropertyName = "des")]
        public string Description { get; set; }
    }
}
