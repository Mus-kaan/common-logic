//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;

namespace Microsoft.Liftr.DataSource.MonitoringSvc
{
    /// <summary>
    /// DataSource for partner resource entity
    /// </summary>
    public interface IPartnerResourceDataSource<TResource> : IResourceEntityDataSource<TResource> where TResource : IPartnerResourceEntity
    {
    }
}
