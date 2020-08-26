//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using System.Threading.Tasks;

namespace Microsoft.Liftr.MarketplaceResource.DataSource
{
    public interface IMarketplaceSaasResourceDataSource
    {
        Task<MarketplaceSaasResourceEntity> AddAsync(MarketplaceSaasResourceEntity entity);

        Task<bool> DeleteAsync(MarketplaceSubscription marketplaceSubscription);

        Task<MarketplaceSaasResourceEntity> GetAsync(MarketplaceSubscription marketplaceSubscription);

        Task<bool> SoftDeleteAsync(MarketplaceSubscription marketplaceSubscription);

        Task<MarketplaceSaasResourceEntity> UpdateAsync(MarketplaceSaasResourceEntity entity);
    }
}