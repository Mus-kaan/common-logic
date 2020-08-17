//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Saas.Interfaces
{
    public interface IWebhookMarketplaceCaller
    {
        /// <summary>
        /// Webhook Authorization for Marketplace
        /// </summary>
        Task<SubscriptionOperation> AuthorizeWebhookWithMarketplaceAsync(MarketplaceSubscription marketplaceSubscription, Guid operationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updating Webhook Operation Result on Partner side to Marketplace.
        /// </summary>
        Task UpdateMarketplaceAsync(MarketplaceSubscription marketplaceSubscription, Guid operationId, OperationUpdate operationUpdate, CancellationToken cancellationToken = default);
    }
}