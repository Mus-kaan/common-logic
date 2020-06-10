//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    /// <summary>
    /// This entity represent each monitored resource
    /// </summary>
    public interface IMonitoringRelationship
    {
        /// <summary>
        /// Resource id of the azure resource which has been monitored
        /// </summary>
        string MonitoredResourceId { get; set; }

        /// <summary>
        /// Object id of the corresponding partner resource entity
        /// </summary>
        string PartnerEntityId { get; set; }

        /// <summary>
        /// Monitored resource tenant id
        /// </summary>
        string TenantId { get; set; }

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
