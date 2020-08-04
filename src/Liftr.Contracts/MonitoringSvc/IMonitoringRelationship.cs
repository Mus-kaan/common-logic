//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    /// <summary>
    /// This entity represent each monitored resource
    /// </summary>
    public interface IMonitoringRelationship : IMonitoringBaseEntity
    {
        /// <summary>
        /// Eventhub namespace AuthorizationRuleId for the monitored resource dignostic settings
        /// </summary>
        string AuthorizationRuleId { get; set; }

        /// <summary>
        /// Eventhub name for the monitored resource dignostic settings
        /// </summary>
        string EventhubName { get; set; }

        /// <summary>
        /// DiagnosticSettingsName of the monitored resource
        /// </summary>
        string DiagnosticSettingsName { get; set; }

        /// <summary>
        /// Time when entity is added
        /// </summary>
        DateTime CreatedAtUTC { get; set; }
    }
}
