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
    public interface IEventHubEntityDataSource
    {
        Task AddAsync(IEventHubEntity entity);

        Task<IEventHubEntity> UpdateAsync(string eventhubNamespaceName, bool ingestEnabled, bool active);

        Task<IEnumerable<IEventHubEntity>> ListAsync();

        Task<IEnumerable<IEventHubEntity>> ListAsync(MonitoringResourceProvider resourceProvider);

        Task<IEnumerable<IEventHubEntity>> ListAsync(MonitoringResourceProvider resourceProvider, string location);

        Task<int> DeleteAsync(MonitoringResourceProvider resourceProvider);

        Task<bool> DeleteAsync(string documentId);
    }
}
