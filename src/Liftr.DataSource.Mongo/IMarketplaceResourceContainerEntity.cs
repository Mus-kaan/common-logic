//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;

namespace Microsoft.Liftr.DataSource.Mongo
{
    /// <summary>
    /// This entity will be used to store the information for the Marketplace resource corresponding to a parent Liftr resource
    /// </summary>
    public interface IMarketplaceResourceContainerEntity : IResourceEntity
    {
        MarketplaceSubscription MarketplaceSubscription { get; set; }
    }
}
