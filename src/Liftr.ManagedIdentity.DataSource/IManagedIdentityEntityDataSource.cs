//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ManagedIdentity.DataSource
{
    public interface IManagedIdentityEntityDataSource : IResourceEntityDataSource<ManagedIdentityEntity>
    {
        Task UpsertAsync(ManagedIdentityEntity entity);

        Task<IAsyncEnumerable<ManagedIdentityEntity>> ListNearExpiryIdentitiesAsync(DateTimeOffset expiryThreshold, CancellationToken cancellationToken = default);
    }
}
