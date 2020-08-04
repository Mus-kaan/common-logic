//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.MonitoringSvc
{
    public interface IMonitoringBaseEntityDataSource<TMonitoringEntity>
        where TMonitoringEntity : IMonitoringBaseEntity
    {
        /// <summary>
        /// Get an existing monitoring record.
        /// </summary>
        Task<TMonitoringEntity> GetAsync(string tenantId, string partnerObjectId, string monitoredResourceId);

        /// <summary>
        /// Retrieve the monitoring records based on the monitored resource Id.
        /// </summary>
        Task<IEnumerable<TMonitoringEntity>> ListByMonitoredResourceAsync(string tenantId, string monitoredResourceId);

        /// <summary>
        /// Retrieve the monitoring records based on the partner resource object Id.
        /// </summary>
        Task<IEnumerable<TMonitoringEntity>> ListByPartnerResourceAsync(string tenantId, string partnerObjectId);

        /// <summary>
        /// Delete the monitoring entity.
        /// </summary>
        Task<int> DeleteAsync(string tenantId, string partnerObjectId = null, string monitoredResourceId = null);
    }
}
