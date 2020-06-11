//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource;
using Microsoft.Liftr.DataSource.Mongo;
using System.Threading.Tasks;

namespace Microsoft.Liftr.MarketplaceResource.DataSource.Interfaces
{
    public interface IMarketplaceResourceContainerEntityDataSource : IResourceEntityDataSource<MarketplaceResourceContainerEntity>
    {
        Task<IMarketplaceResourceContainerEntity> GetEntityForMarketplaceSubscriptionAsync(MarketplaceSubscription marketplaceSubscription);
    }
}
