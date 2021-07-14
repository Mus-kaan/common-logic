//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource
{
    public interface IResourceEntityDataSource<TResource> where TResource : IResourceEntity
    {
        Task<TResource> AddAsync(TResource entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get an entity by the entity Id (Mongo DB Object Id).
        /// </summary>
        Task<TResource> GetAsync(string entityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// List all the entities for a specific ARM resource Id.
        /// </summary>
        Task<IEnumerable<TResource>> ListAsync(string resourceId, bool showActiveOnly = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// List all available entities.
        /// </summary>
        Task<IEnumerable<TResource>> ListAsync(bool showActiveOnly = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Find an entity by the entity Id (Mongo DB Object Id) and then mark it as inactive and change the provisioning state.
        /// </summary>
        Task<bool> SoftDeleteAsync(string entityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete an entity by the entity Id (Mongo DB Object Id).
        /// </summary>
        Task<bool> DeleteAsync(string entityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update a resource entity
        /// </summary>
        Task UpdateAsync(TResource entity, CancellationToken cancellationToken = default);
    }
}
