//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Utilities;
using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.StatusStore.Blob
{
    public class StatusRecord : WriterMetaData, IStatusRecord
    {
        public StatusRecord()
        {
        }

        public StatusRecord(IWriterMetaData writerMeta)
        {
            if (writerMeta == null)
            {
                throw new ArgumentNullException(nameof(writerMeta));
            }

            MachineName = writerMeta.MachineName;
            RunningSessionId = writerMeta.RunningSessionId;
            ProcessStartTime = writerMeta.ProcessStartTime;
            SubscriptionId = writerMeta.SubscriptionId;
            ResourceGroup = writerMeta.ResourceGroup;
            VMName = writerMeta.VMName;
            Region = writerMeta.Region;
        }

        [JsonProperty("key", Required = Required.Always)]
        public string Key { get; set; }

        [JsonProperty("ts", Required = Required.Always)]
        [JsonConverter(typeof(ZuluDateTimeConverter))]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("correlationId", NullValueHandling = NullValueHandling.Ignore)]
        public string CorrelationId { get; set; }

        [JsonProperty("b64value", Required = Required.Always)]
        public string Value { get; set; }
    }
}
