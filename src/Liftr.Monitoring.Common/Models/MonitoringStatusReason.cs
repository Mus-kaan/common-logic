//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Monitoring.Common.Models
{
    /// <summary>
    /// The possible reasons for the monitoring status of a resource.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MonitoringStatusReason
    {
        /// <summary>
        /// Resource is monitored because it was captured by the monitoring rules.
        /// </summary>
        CapturedByRules,

        /// <summary>
        /// Resource is not monitored because it was not captured by the monitoring rules.
        /// </summary>
        NotCapturedByRules,

        /// <summary>
        /// Resource is not monitored because the resource type does not support logs at diagnostic settings.
        /// </summary>
        ResourceTypeNotSupported,

        /// <summary>
        /// Resource is not monitored because the location is not supported.
        /// </summary>
        LocationNotSupported,

        /// <summary>
        /// Resource has reached the limit of diagnostic settings (currently 5).
        /// </summary>
        DiagnosticSettingsLimitReached,

        /// <summary>
        /// The scope is locked and blocking interactions with the diagnostic settings.
        /// </summary>
        ScopeLocked,

        /// <summary>
        /// 409 conflict status was returned when trying to create the diagnostic setting.
        /// </summary>
        ConflictStatus,

        /// <summary>
        /// Other errors happened when trying to create the diagnostic setting. Requires support.
        /// </summary>
        Other,
    }

    /// <summary>
    /// Extensions for the MonitoringStatusReason enum.
    /// </summary>
    public static class MonitoringStatusReasonExtensions
    {
        /// <summary>
        /// Converts the enum value to its correspondent string.
        /// </summary>
        public static string GetReasonName(this MonitoringStatusReason reason)
        {
            return reason.ToString();
        }

        /// <summary>
        /// Gets the monitoring state of the resource based on the MonitoringStatusReason value.
        /// </summary>
        public static bool ActiveMonitoringStateForResource(this MonitoringStatusReason reason)
        {
            if (reason == MonitoringStatusReason.CapturedByRules)
            {
                // Only reason associated to resources successfully monitored.
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
