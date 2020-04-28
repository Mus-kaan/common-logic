//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource;
using Microsoft.Liftr.MarketplaceResource.DataSource.Models;
using System.Threading.Tasks;

namespace Microsoft.Liftr.MarketplaceResource.DataSource.Interfaces
{
    public interface IMarketplaceResourceEntityDataSource : IResourceEntityDataSource<MarketplaceResourceEntity>
    {
        Task<IMarketplaceResourceEntity> GetEntityForMarketplaceSubscriptionAsync(MarketplaceSubscription marketplaceSubscription);
    }
}
