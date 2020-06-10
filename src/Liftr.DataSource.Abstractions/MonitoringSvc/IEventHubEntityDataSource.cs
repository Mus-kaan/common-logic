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

        Task<IEnumerable<IEventHubEntity>> ListAsync(string resourceProvider);

        Task<IEnumerable<IEventHubEntity>> ListAsync(string resourceProvider, string location);

        Task<int> DeleteAsync(string resourceProvider);
    }
}
