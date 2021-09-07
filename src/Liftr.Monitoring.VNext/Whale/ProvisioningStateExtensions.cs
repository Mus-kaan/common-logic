//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System.Collections.Generic;

namespace Liftr.Monitoring.VNext.Whale
{
    public static class ProvisioningStateExtensions
    {
        private static HashSet<ProvisioningState> s_stopMonitoringStates = new HashSet<ProvisioningState>()
        {
            ProvisioningState.Deleting,
            ProvisioningState.Canceled,
            ProvisioningState.Failed,
            ProvisioningState.Deleted,
        };

        private static HashSet<ProvisioningState> s_notStartMonitoringStates = new HashSet<ProvisioningState>()
        {
            ProvisioningState.Accepted,
            ProvisioningState.Creating,
            ProvisioningState.NotSpecified,
            ProvisioningState.Updating,
        };

        /// <summary>
        /// Indicates if the Whale should stop auto-monitoring the resource.
        /// </summary>
        public static bool ShouldStopMonitoringResource(this ProvisioningState provisioningState)
        {
            return s_stopMonitoringStates.Contains(provisioningState);
        }

        /// <summary>
        /// Indicates if the Whale should not process the auto-monitoring message for the resource.
        /// </summary>
        public static bool ShouldNotStartMonitoringResource(this ProvisioningState provisioningState)
        {
            return s_notStartMonitoringStates.Contains(provisioningState);
        }
    }
}