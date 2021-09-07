//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;

namespace Microsoft.Liftr.Monitoring.VNext.Whale.Models
{
    /// <summary>
    /// Monitor resource details extracted from ARM resource type of monitor that will be used in Whale worker.
    /// </summary>
    public class MonitorResourceDetails
    {
        /// <summary>
        /// Object id for mapping the MonitoringRelationship and ParterResource entities.
        /// </summary>
        public string MonitoringPartnerEntityId { get; set; }

        /// <summary>
        /// The monitoring status of the resource.
        /// </summary>
        public MonitoringStatus MonitoringStatus { get; set; }

        /// <summary>
        /// Provisioning state of the resource.
        /// </summary>
        public ProvisioningState ProvisioningState { get; set; }

        /// <summary>
        ///     The location of the resource.
        /// </summary>
        public string Location { get; set; }
    }
}
