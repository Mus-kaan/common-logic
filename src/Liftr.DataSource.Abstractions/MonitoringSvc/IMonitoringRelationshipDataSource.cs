//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.MonitoringSvc
{
    /// <summary>
    /// DataSource for managing monitored entity
    /// </summary>
    public interface IMonitoringRelationshipDataSource
    {
        /// <summary>
        /// Add the monitoring relationship record.
        /// </summary>
        Task<IMonitoringRelationship> AddAsync(IMonitoringRelationship entity);

        /// <summary>
        /// Get an existing monitoring relationship record.
        /// </summary>
        Task<IMonitoringRelationship> GetAsync(string tenantId, string partnerObjectId, string monitoredResourceId);

        /// <summary>
        /// Retrieve the monitoring relationship records based on the monitored resource Id.
        /// </summary>
        Task<IEnumerable<IMonitoringRelationship>> ListByMonitoredResourceAsync(string tenantId, string monitoredResourceId);

        /// <summary>
        /// Retrieve the monitoring relationship records based on the partner resource object Id.
        /// </summary>
        Task<IEnumerable<IMonitoringRelationship>> ListByPartnerResourceAsync(string tenantId, string partnerResourceObjectId);

        /// <summary>
        /// Delete the monitoring relationship.
        /// </summary>
        Task<int> DeleteAsync(string tenantId, string partnerResourceObjectId = null, string monitoredResourceId = null);
    }
}
