//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.MonitoringSvc
{
    /// <summary>
    /// DataSource for managing monitored entity.
    /// </summary>
    public interface IMonitoringRelationshipDataSource<TMonitoringEntity>
        : IMonitoringBaseEntityDataSource<TMonitoringEntity> where TMonitoringEntity : IMonitoringRelationship
    {
        /// <summary>
        /// Add the monitoring relationship record.
        /// </summary>
        Task<IMonitoringRelationship> AddAsync(IMonitoringRelationship entity);
    }
}
