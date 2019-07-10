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

        Task<TResource> GetEntityAsync(string subscriptionId, string resourceGroup, string name);

        Task<TResource> GetEntityAsync(string name);

        Task<IList<TResource>> ListEntitiesAsync(string subscriptionId, string resourceGroup);

        Task<bool> DeleteEntityAsync(string subscriptionId, string resourceGroup, string name);
    }
}
