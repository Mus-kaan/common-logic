//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.MonitoringSvc
{
    /// <summary>
    /// DataSource for managing azure vm extension details
    /// </summary>
    public interface IMonitoringSvcVMExtensionDetailsEntityDataSource
    {
        Task<IMonitoringSvcVMExtensionDetailsEntity> GetEntityAsync(string resourceProviderType, string operatingSystem);
    }
}
