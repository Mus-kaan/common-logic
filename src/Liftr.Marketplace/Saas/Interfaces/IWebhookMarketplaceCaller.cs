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
        /// Calling Get Operation API on Operation Id received from Webhook payload for authorization
        /// </summary>
        /// <param name="marketplaceSubscription">Marketplace Subscription</param>
        /// <param name="operationId">Operation Id</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>SubscriptionOperation</returns>
        Task<SubscriptionOperation> AuthorizeWebhookWithMarketplaceAsync(MarketplaceSubscription marketplaceSubscription, Guid operationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updating Webhook Operation Result (Success/Failure) on Partner side to Marketplace.
        /// </summary>
        /// <param name="marketplaceSubscription">Marketplace Subscription</param>
        /// <param name="operationId">Operation Id</param>
        /// <param name="operationUpdate">Operation Id</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        Task UpdateMarketplaceAsync(MarketplaceSubscription marketplaceSubscription, Guid operationId, OperationUpdate operationUpdate, CancellationToken cancellationToken = default);
    }
}