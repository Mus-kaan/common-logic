//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.MarketplaceResource.DataSource.Models;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource;
using System.Threading.Tasks;

namespace Liftr.MarketplaceResource.DataSource.Interfaces
{
    public interface IMarketplaceResourceEntityDataSource : IResourceEntityDataSource<MarketplaceResourceEntity>
    {
        Task<IMarketplaceResourceEntity> GetEntityForMarketplaceSubscriptionAsync(MarketplaceSubscription marketplaceSubscription);
    }
}
