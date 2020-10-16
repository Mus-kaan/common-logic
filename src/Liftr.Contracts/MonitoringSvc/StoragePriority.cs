//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StoragePriority
    {
        Primary,
        Backup,
    }
}
