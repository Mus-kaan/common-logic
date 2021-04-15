//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Utilities;
using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.StatusStore.Blob
{
    public class WriterMetaData : IWriterMetaData
    {
        [JsonProperty("machine", Required = Required.Always)]
        public string MachineName { get; set; }

        [JsonProperty("sessionId", Required = Required.Always)]
        public string RunningSessionId { get; set; }

        [JsonProperty("startAt", Required = Required.Always)]
        [JsonConverter(typeof(ZuluDateTimeConverter))]
        public DateTime ProcessStartTime { get; set; }

        [JsonProperty("subId", NullValueHandling = NullValueHandling.Ignore)]
        public string SubscriptionId { get; set; }

        [JsonProperty("rg", NullValueHandling = NullValueHandling.Ignore)]
        public string ResourceGroup { get; set; }

        [JsonProperty("vm", NullValueHandling = NullValueHandling.Ignore)]
        public string VMName { get; set; }

        [JsonProperty("region", NullValueHandling = NullValueHandling.Ignore)]
        public string Region { get; set; }
    }
}
