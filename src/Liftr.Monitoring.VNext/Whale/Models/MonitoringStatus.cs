//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Monitoring.VNext.Whale.Models
{
    /// <summary>
    /// Flag specifying if the resource monitoring is enabled or disabled.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MonitoringStatus
    {
        /// <summary>
        /// Send logs and metrics (according to the selected options) for this monitor resource.
        /// </summary>
        Enabled,

        /// <summary>
        /// Do not send logs and metrics for this monitor resource, regardless of the selected options.
        /// </summary>
        Disabled,
    }
}
