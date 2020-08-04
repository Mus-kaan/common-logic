//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    /// <summary>
    /// This entity represents the monitoring status of resources captured by filter rules.
    /// </summary>
    public interface IMonitoringStatus : IMonitoringBaseEntity
    {
        /// <summary>
        /// Monitoring status for the Azure resource.
        /// </summary>
        bool IsMonitored { get; set; }

        /// <summary>
        /// Code of the reason for the monitoring status.
        /// </summary>
        string Reason { get; set; }

        /// <summary>
        /// Last modified timestamp for the entity.
        /// </summary>
        DateTime LastModifiedAtUTC { get; set; }
    }
}
