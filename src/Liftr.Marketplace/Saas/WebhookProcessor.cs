//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Logging;
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
        private readonly IWebhookMarketplaceCaller _marketplaceCaller;
        private readonly IWebhookHandler _webhookHandler;
        private readonly ILogger _logger;

        public WebhookProcessor(
            IWebhookMarketplaceCaller marketplaceCaller,
            IWebhookHandler webhookHandler,
            ILogger logger)
        {
            _marketplaceCaller = marketplaceCaller ?? throw new ArgumentNullException(nameof(marketplaceCaller));
            _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public WebhookProcessor(IWebhookMarketplaceCaller marketplaceCaller, IWebhookHandler webhookHandler)
            : this(marketplaceCaller, webhookHandler, LoggerFactory.ConsoleLogger)
        {
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

            if (payload.OperationId == Guid.Empty)
            {
                throw new FormatException("OperationId Guid is not in required Format");
            }

            if (!Enum.IsDefined(typeof(WebhookAction), payload.Action))
            {
                throw new InvalidOperationException("Invalid Action type");
            }

            if (payload.MarketplaceSubscription is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            try
            {
                // Since webhook endpoint is unauthorized, call the operations api to confirm that the operation has been initaited from the marketplace
                var operation = await _marketplaceCaller.AuthorizeWebhookWithMarketplaceAsync(payload.MarketplaceSubscription, payload.OperationId, cancellationToken);
                _logger.Information("Successfully retrieved details for operation {operation} for webhook action {action} on Subsciption:{subscriptionId}", payload.OperationId, payload.Action, payload.MarketplaceSubscription);
            }
            catch (MarketplaceException ex)
            {
                var errorMessage = $"Failed to get operation for operation id: {payload.OperationId}. Cannot proceed with processing the webhook with Exception {ex.Message}";
                _logger.Error(ex, errorMessage);
                throw new InvalidOperationException(errorMessage, ex);
            }

            try
            {
                OperationUpdateStatus operationStatus;
                bool patchRequired = true;
                switch (payload.Action)
                {
                    case WebhookAction.Unsubscribe:
                        operationStatus = await _webhookHandler.ProcessDeleteAsync(payload);
                        patchRequired = false;
                        break;
                    case WebhookAction.ChangePlan:
                        operationStatus = await _webhookHandler.ProcessChangePlanAsync(payload);
                        break;
                    case WebhookAction.ChangeQuantity:
                        operationStatus = await _webhookHandler.ProcessChangeQuantityAsync(payload);
                        break;
                    case WebhookAction.Suspend:
                        operationStatus = await _webhookHandler.ProcessSuspendAsync(payload);
                        patchRequired = false;
                        break;
                    case WebhookAction.Reinstate:
                        operationStatus = await _webhookHandler.ProcessReinstateAsync(payload);
                        break;
                    default:
                        _logger.Error("Action {action} is not supported", payload.Action, payload.MarketplaceSubscription);
                        throw new MarketplaceException($"Action {payload.Action} is not supported");
                }

                // After Success/Failure, Patch the operation to Marketplace. Patch is not required for Delete and Suspend action
                if (patchRequired)
                {
                    await _marketplaceCaller.UpdateMarketplaceAsync(
                    payload.MarketplaceSubscription,
                    payload.OperationId,
                    new OperationUpdate(payload.PlanId, payload.Quantity, operationStatus),
                    cancellationToken);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to Handle Webhook Notification. Exception is ex: {ex.Message}";
                _logger.Error(ex, errorMessage);
                throw new MarketplaceException(errorMessage, ex);
            }
        }
    }
}