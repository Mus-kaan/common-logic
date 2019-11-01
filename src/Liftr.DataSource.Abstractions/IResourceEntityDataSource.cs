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
        Task<TResource> AddEntityAsync(TResource entity);

        /// <summary>
        /// Get an entity by the entity Id (Mongo DB Object Id).
        /// </summary>
        Task<TResource> GetEntityAsync(string entityId);

        Task<IEnumerable<TResource>> ListEntitiesByResourceIdAsync(string resourceId);
    }
}
