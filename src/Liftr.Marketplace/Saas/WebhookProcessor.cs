//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Saas
{
    /// <summary>
    /// Webhook flow for Marketplace
    /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/create-new-saas-offer#technical-configuration
    /// </summary>
    public class WebhookProcessor : IWebhookProcessor
    {
        private readonly IMarketplaceFulfillmentClient _fulfillmentClient;
        private readonly IWebhookHandler _webhookHandler;
        private readonly ILogger _logger;

        public WebhookProcessor(
            IMarketplaceFulfillmentClient fulfillmentClient,
            IWebhookHandler webhookHandler,
            ILogger logger)
        {
            _fulfillmentClient = fulfillmentClient;
            _webhookHandler = webhookHandler;
            _logger = logger;
        }

        /// <summary>
        /// On receiving an update it gets the operation details using operation id
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#implementing-a-webhook-on-the-saas-service
        /// After performing the update it patches the operation to Marketplace to signal success or failure
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#provisioning-for-update-when-its-initiated-from-the-marketplace
        /// </summary>
        /// <param name="payload">Webhook notification payload from the marketplace</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ProcessWebhookNotificationAsync(
            WebhookPayload payload,
            CancellationToken cancellationToken = default)
        {
            if (payload is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            try
            {
                // Since webhook endpoint is unauthenticated, call the operations api to confirm that the operation has been initaited from the marketplace
                var operation = await _fulfillmentClient.GetOperationAsync(payload.MarketplaceSubscription, payload.OperationId, cancellationToken);
                _logger.Information("Successfully retrieved details for operation {operation} for webhook action {action} on Subsciption:{subscriptionId}", payload.OperationId, payload.Action, payload.MarketplaceSubscription);
            }
            catch (MarketplaceHttpException ex)
            {
                _logger.Error(ex, "Failed to get operation for operation id: {operationId}. Cannot proceed with processing the webhook.", payload.OperationId);
                throw;
            }

            switch (payload.Action)
            {
                case WebhookAction.Unsubscribe:
                    var operationStatus = await _webhookHandler.ProcessDeleteAsync(payload.MarketplaceSubscription);

                    // Once the delete is successful, patch the operation
                    await _fulfillmentClient.UpdateOperationAsync(
                        payload.MarketplaceSubscription,
                        payload.OperationId,
                        new OperationUpdate(payload.PlanId, payload.Quantity, operationStatus));
                    break;
                default:
                    _logger.Error("Action {action} is not supported", payload.Action, payload.MarketplaceSubscription);
                    throw new MarketplaceHttpException($"Action {payload.Action} is not supported");
            }
        }
    }
}
