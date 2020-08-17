//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Saas
{
    public class WebhookMarketplaceCaller : IWebhookMarketplaceCaller
    {
        private readonly IMarketplaceFulfillmentClient _fulfillmentClient;
        private readonly ILogger _logger;

        public WebhookMarketplaceCaller(IMarketplaceFulfillmentClient fulfillmentClient, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fulfillmentClient = fulfillmentClient ?? throw new ArgumentNullException(nameof(fulfillmentClient));
        }

        public async Task<SubscriptionOperation> AuthorizeWebhookWithMarketplaceAsync(MarketplaceSubscription marketplaceSubscription, Guid operationId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _fulfillmentClient.GetOperationAsync(marketplaceSubscription, operationId, cancellationToken);
            }
            catch (MarketplaceException ex)
            {
                var errorMessage = $"Failed to Authorize with Marketplace! Error Message: {ex.Message}";
                _logger.Error(ex, errorMessage);
                throw new MarketplaceException(errorMessage, ex);
            }
        }

        public async Task UpdateMarketplaceAsync(MarketplaceSubscription marketplaceSubscription, Guid operationId, OperationUpdate operationUpdate, CancellationToken cancellationToken = default)
        {
            try
            {
                await _fulfillmentClient.UpdateOperationAsync(
                        marketplaceSubscription,
                        operationId,
                        operationUpdate);
            }
            catch (MarketplaceException ex)
            {
                var errorMessage = $"Failed to Update to Marketplace! Error Message: {ex.Message}";
                _logger.Error(ex, errorMessage);
                throw new MarketplaceException(errorMessage, ex);
            }
        }
    }
}