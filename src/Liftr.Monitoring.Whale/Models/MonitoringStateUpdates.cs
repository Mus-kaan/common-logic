//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Monitoring.Whale.Models
{
    /// <summary>
    /// Definition for storing resources that should be added/removed from monitoring state.
    /// </summary>
    public class MonitoringStateUpdates
    {
        /// <summary>
        /// Resources that we should start monitoring.
        /// </summary>
        public IEnumerable<MonitoredResource> ResourcesToStartMonitoring { get; set; }

        /// <summary>
        /// Resources that we should stop monitoring.
        /// </summary>
        public IEnumerable<MonitoredResource> ResourcesToStopMonitoring { get; set; }

        /// <summary>
        /// Resources that we should stop tracking.
        /// </summary>
        public IEnumerable<MonitoredResource> ResourcesToStopTracking { get; set; }
    }
}
