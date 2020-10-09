//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.ACIS.Relay
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LogLevel
    {
        Error,
        Warning,
        Info,
        Verbose,
    }

    public class LogEntry
    {
        public string TimeStamp { get; set; }

        public LogLevel Level { get; set; }

        public string Message { get; set; }

        public string Machine { get; set; }
    }
}
