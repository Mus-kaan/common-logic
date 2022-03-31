//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.DBService.Contracts.Interfaces
{
    // This needs to be moved to liftr common
    public interface IOrchestratorEntityService<T>
    {
        Task<T> CreateOrchestratorEntityAsync(T entity);

        Task<T> GetOrchestratorEntityAsync(string serviceTreeId, string repoName);

        Task<T> UpdateOrchestratorEntityAsync(T entity);
    }
}
