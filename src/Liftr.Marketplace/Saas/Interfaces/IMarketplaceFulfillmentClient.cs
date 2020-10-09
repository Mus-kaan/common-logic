//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Saas.Interfaces
{
    /// <summary>
    /// Client used to make the Marketplace Saas Fulfillment Requests
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2
    /// </summary>
    public interface IMarketplaceFulfillmentClient
    {
        /// <summary>
        /// Resolve the subscription id for a Marketplace Saas subscription
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#resolve-a-subscription
        /// </summary>
        /// <remarks>When a new Saas resource is created, it is associated with a unique subscription id, which is an identifier
        /// for the further interaction with the marketplace APIs</remarks>
        /// <param name="marketplaceToken">The resolve token which will be sent to marketplace to get the subscription id</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Resolved Marketplace subscription</returns>
        Task<ResolvedMarketplaceSubscription> ResolveSaaSSubscriptionAsync(string marketplaceToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Activate the subscription with the corresponding plan and quantity
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#activate-a-subscription
        /// </summary>
        /// <param name="activateSubscriptionRequest">Request containing the Subscription Id to be activated alongwith plan and quantity</param>
        /// <param name="token"></param>
        /// <returns>Activate the marketplace subscription</returns>
        Task ActivateSaaSSubscriptionAsync(ActivateSubscriptionRequest activateSubscriptionRequest, CancellationToken token = default);

        /// <summary>
        /// Get all pending operations for the subscription
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#list-outstanding-operations
        /// </summary>
        /// <param name="marketplaceSubscription">Marketplace Subscription</param>
        /// <param name="cancellationToken"></param>
        /// <returns>All pending operations for this subscription</returns>
        Task<IEnumerable<SubscriptionOperation>> ListPendingOperationsAsync(MarketplaceSubscription marketplaceSubscription, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the operation for the subscription
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#get-operation-status
        /// </summary>
        /// <param name="marketplaceSubscription">Marketplace Subscription</param>
        /// <param name="operationId">Identifier for the operation</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Get the details of the operation</returns>
        Task<SubscriptionOperation> GetOperationAsync(MarketplaceSubscription marketplaceSubscription, Guid operationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Patch the operation to signal success or failure of the operation
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#update-the-status-of-an-operation
        /// </summary>
        /// <param name="marketplaceSubscription">Marketplace Subscription</param>
        /// <param name="operationId">Identifier for the operation</param>
        /// <param name="operationUpdate">Details of the update</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateOperationAsync(MarketplaceSubscription marketplaceSubscription, Guid operationId, OperationUpdate operationUpdate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete the marketplace subscription
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#delete-a-subscription
        /// </summary>
        /// <param name="marketplaceSubscription">Marketplace Subscription</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteSubscriptionAsync(MarketplaceSubscription marketplaceSubscription, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the marketplace subscription details
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#get-subscription
        /// </summary>
        /// <param name="marketplaceSubscription">Marketplace Subscription</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MarketplaceSubscriptionDetails> GetSubscriptionAsync(MarketplaceSubscription marketplaceSubscription, CancellationToken cancellationToken = default);
    }
}