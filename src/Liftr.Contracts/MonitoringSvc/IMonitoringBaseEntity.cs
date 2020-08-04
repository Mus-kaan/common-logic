//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    public interface IMonitoringBaseEntity
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
    }
}
