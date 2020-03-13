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
    public interface IMonitoringSvcMonitoredEntityDataSource
    {
        /// <summary>
        /// Add monitored entity, basically when we perform StartMonitor on any azure resource, we will add entity here with its event hub details so logforwarder can use it
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<IMonitoringSvcMonitoredEntity> AddEntityAsync(IMonitoringSvcMonitoredEntity entity);

        /// <summary>
        /// Retrieves first monitored entity for given monitored resource id
        /// </summary>
        /// <param name="monitoredResourceId"></param>
        /// <returns></returns>
        Task<IMonitoringSvcMonitoredEntity> GetEntityAsync(string monitoredResourceId);

        /// <summary>
        /// Retrieves monitored entities for given monitored resource id
        /// </summary>
        /// <param name="monitoredResourceId"></param>
        /// <returns></returns>
        Task<IEnumerable<IMonitoringSvcMonitoredEntity>> GetEntitiesAsync(string monitoredResourceId);

        /// <summary>
        /// Retrieves all monitored entities by given monitoring resource id
        /// </summary>
        /// <param name="monitoringResourceId"></param>
        /// <returns></returns>
        Task<IEnumerable<IMonitoringSvcMonitoredEntity>> GetAllEntityByMonitoringResourceIdAsync(string monitoringResourceId);

        /// <summary>
        /// Deletes monitored resource entity
        /// </summary>
        /// <param name="monitoredResourceId"></param>
        /// <returns></returns>
        Task DeleteEntityAsync(string monitoredResourceId);

        /// <summary>
        /// Retrieves all monitored entities
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<IMonitoringSvcMonitoredEntity>> GetAllEntityAsync();

        /// <summary>
        /// Get all distinct monitoring resource ids.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<string>> GetAllMonitoringRresourcesAsync();
    }
}
