//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.MonitoringSvc
{
    /// <summary>
    /// DataSource to perform Eventhub retrieval operations.
    /// Note -> update to this collection would be only through deployment scripts/jarvis actions
    /// </summary>
    public interface IMonitoringSvcEventHubEntityDataSource
    {
        /// <summary>
        /// Retrives EventHub entity for given partner and location, event hub usage is per partner and per location
        /// </summary>
        /// <param name="partnerSvcType"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        Task<IMonitoringSvcEventHubEntity> GetEntityAsync(MonitoringSvcType partnerSvcType, string location);

        /// <summary>
        /// Retrieves all enabled Eventhub entities configured to forward logs/metrics
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<IMonitoringSvcEventHubEntity>> ListEntityAsync();
    }
}
