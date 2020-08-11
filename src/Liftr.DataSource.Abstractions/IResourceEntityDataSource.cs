//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource
{
    public interface IResourceEntityDataSource<TResource> where TResource : IResourceEntity
    {
        Task<TResource> AddAsync(TResource entity);

        /// <summary>
        /// Get an entity by the entity Id (Mongo DB Object Id).
        /// </summary>
        Task<TResource> GetAsync(string entityId);

        /// <summary>
        /// List all the entities for a specific ARM resource Id.
        /// </summary>
        Task<IEnumerable<TResource>> ListAsync(string resourceId, bool showActiveOnly = true);

        /// <summary>
        /// Find an entity by the entity Id (Mongo DB Object Id) and then mark it as inactive and change the provisioning state.
        /// </summary>
        Task<bool> SoftDeleteAsync(string entityId);

        /// <summary>
        /// Delete an entity by the entity Id (Mongo DB Object Id).
        /// </summary>
        Task<bool> DeleteAsync(string entityId);

        /// <summary>
        /// Update a resource entity
        /// </summary>
        Task<TResource> UpdateAsync(TResource entity);
    }
}
