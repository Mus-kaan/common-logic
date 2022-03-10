//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.VNetInjection.DataSource.Mongo
{
    public interface IVNetInjectionEntityDataSource : IResourceEntityDataSource<VNetInjectionEntity>
    {
        Task<VNetInjectionEntity> UpsertAsync(VNetInjectionEntity entity, CancellationToken cancellationToken = default);
    }
}
