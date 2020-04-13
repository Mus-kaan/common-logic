//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;

namespace Microsoft.Liftr.Contracts
{
    public interface IMarketplaceResourceEntity : IResourceEntity
    {
        MarketplaceSubscription MarketplaceSubscription { get; set; }

        string SaasResourceId { get; set; }
    }
}
