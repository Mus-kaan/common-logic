//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ManagedIdentity.DataSource
{
    public interface IManagedIdentityEntityDataSource : IResourceEntityDataSource<ManagedIdentityEntity>
    {
        Task UpsertAsync(ManagedIdentityEntity entity);
    }
}
