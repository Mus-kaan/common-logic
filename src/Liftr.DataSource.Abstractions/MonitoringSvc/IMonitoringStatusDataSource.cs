//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.MonitoringSvc
{
    /// <summary>
    /// DataSource for managing monitoring status entity.
    /// </summary>
    public interface IMonitoringStatusDataSource<TMonitoringEntity>
        : IMonitoringBaseEntityDataSource<TMonitoringEntity> where TMonitoringEntity : IMonitoringStatus
    {
        /// <summary>
        /// Add or update the monitoring status record.
        /// </summary>
        Task<IMonitoringStatus> AddOrUpdateAsync(IMonitoringStatus entity);
    }
}
