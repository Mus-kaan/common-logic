//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.MonitoringSvc
{
    public interface IStorageEntityDataSource
    {
        Task AddAsync(IStorageEntity entity);

        Task<IEnumerable<IStorageEntity>> ListAsync(StoragePriority priority, string logForwarderRegion = null, StorageType? type = null);

        Task<bool> DeleteAsync(string documentId);
    }
}
