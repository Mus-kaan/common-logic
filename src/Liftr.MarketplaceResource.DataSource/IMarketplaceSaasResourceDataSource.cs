//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.MarketplaceResource.DataSource
{
    public interface IMarketplaceSaasResourceDataSource
    {
        Task<MarketplaceSaasResourceEntity> AddAsync(MarketplaceSaasResourceEntity entity);

        Task<bool> DeleteAsync(MarketplaceSubscription marketplaceSubscription);

        Task<PaginatedResponse> GetPaginatedResourcesAsync(int pageSize = 10, DateTime? timeStamp = null, bool showActiveOnly = true);

        Task<MarketplaceSaasResourceEntity> GetAsync(MarketplaceSubscription marketplaceSubscription);

        Task<bool> SoftDeleteAsync(MarketplaceSubscription marketplaceSubscription);

        Task<MarketplaceSaasResourceEntity> UpdateAsync(MarketplaceSaasResourceEntity entity);
    }
}