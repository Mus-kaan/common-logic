//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;
using Microsoft.Liftr.Marketplace.Saas.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Saas
{
    /// <inheritdoc/>
    public class MarketplaceFulfillmentClient : IMarketplaceFulfillmentClient
    {
        private readonly ILogger _logger;
        private readonly MarketplaceRestClient _marketplaceRestClient;

        internal MarketplaceFulfillmentClient(MarketplaceRestClient marketplaceRestClient, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _marketplaceRestClient = marketplaceRestClient ?? throw new ArgumentNullException(nameof(marketplaceRestClient));
        }

        public async Task<ResolvedMarketplaceSubscription> ResolveSaaSSubscriptionAsync(
            string marketplaceToken,
            CancellationToken cancellationToken = default)
        {
            using var op = _logger.StartTimedOperation(nameof(ResolveSaaSSubscriptionAsync));
            var requestPath = "/api/saas/subscriptions/resolve";

            try
            {
                var response = await _marketplaceRestClient.SendRequestAsync<ResolvedMarketplaceSubscription>(
                HttpMethod.Post,
                requestPath,
                new Dictionary<string, string>() { { "x-ms-marketplace-token", marketplaceToken } },
                string.Empty,
                cancellationToken);

                _logger.Information("Subscription: {@resolvedSubscription} resolved", response);
                op.SetResultDescription($"Subscription: {response} resolved");
                return response;
            }
            catch (MarketplaceException ex)
            {
                _logger.Error(ex, "Failed to resolve subscription");
                op.FailOperation("Failed to resolve subscription");
                throw new MarketplaceException("Failed to resolve subscription", ex);
            }
        }

        public async Task ActivateSaaSSubscriptionAsync(
            ActivateSubscriptionRequest activateSubscriptionRequest,
            CancellationToken token = default)
        {
            if (activateSubscriptionRequest is null)
            {
                throw new ArgumentNullException(nameof(activateSubscriptionRequest));
            }

            using var op = _logger.StartTimedOperation(nameof(ActivateSaaSSubscriptionAsync));
            var requestPath = $"/api/saas/subscriptions/{activateSubscriptionRequest.MarketplaceSubscription}/activate";

            try
            {
                await _marketplaceRestClient.SendRequestAsync<string>(
                HttpMethod.Post,
                requestPath,
                content: activateSubscriptionRequest.ToJObject(),
                cancellationToken: token);

                _logger.Information("Subscription: {@subscription} activated", activateSubscriptionRequest.MarketplaceSubscription);
                op.SetResultDescription($"Subscription: {activateSubscriptionRequest.MarketplaceSubscription} activated");
            }
            catch (MarketplaceException ex)
            {
                _logger.Error(ex, "Subscription: {@subscription} could not be activated", activateSubscriptionRequest.MarketplaceSubscription);
                op.FailOperation($"Subscription: {activateSubscriptionRequest.MarketplaceSubscription} could not be activated");
                throw new MarketplaceException($"Failed to activate subscription: {activateSubscriptionRequest.MarketplaceSubscription}", ex);
            }
        }

        public async Task<SubscriptionOperation> GetOperationAsync(
            MarketplaceSubscription marketplaceSubscription,
            Guid operationId,
            CancellationToken cancellationToken = default)
        {
            using var op = _logger.StartTimedOperation(nameof(GetOperationAsync));
            var requestPath = $"/api/saas/subscriptions/{marketplaceSubscription}/operations/{operationId}";

            try
            {
                var operation = await _marketplaceRestClient.SendRequestAsync<SubscriptionOperation>(
                    HttpMethod.Get,
                    requestPath,
                    cancellationToken: cancellationToken);

                var message = $"Subscription: {marketplaceSubscription}: Successfully retrieved operation for operation id: {operationId} with status {operation.Status}";
                _logger.Information(message);
                op.SetResultDescription(message);
                return operation;
            }
            catch (MarketplaceException ex)
            {
                var errorMessage = $"Subscription: {marketplaceSubscription}: Failed to get operation for operation id {operationId}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw new MarketplaceException(errorMessage, ex);
            }
        }

        public async Task<IEnumerable<SubscriptionOperation>> ListPendingOperationsAsync(
            MarketplaceSubscription marketplaceSubscription,
            CancellationToken cancellationToken = default)
        {
            using var op = _logger.StartTimedOperation(nameof(ListPendingOperationsAsync));
            var requestPath = $"/api/saas/subscriptions/{marketplaceSubscription}/operations";

            try
            {
                var operations = await _marketplaceRestClient.SendRequestAsync<IEnumerable<SubscriptionOperation>>(
                    HttpMethod.Get,
                    requestPath,
                    cancellationToken: cancellationToken);

                var message = $"Subscription: {marketplaceSubscription}: Successfully retrieved {operations.Count()} operations";
                _logger.Information(message);
                op.SetResultDescription(message);
                return operations;
            }
            catch (MarketplaceException ex)
            {
                var errorMessage = $"Subscription {marketplaceSubscription}: Failed to get operations";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw new MarketplaceException(errorMessage, ex);
            }
        }

        public async Task UpdateOperationAsync(
            MarketplaceSubscription marketplaceSubscription,
            Guid operationId,
            OperationUpdate operationUpdate,
            CancellationToken cancellationToken = default)
        {
            if (operationUpdate is null)
            {
                throw new ArgumentNullException(nameof(operationUpdate));
            }

            using var op = _logger.StartTimedOperation(nameof(UpdateOperationAsync));
            var requestPath = $"/api/saas/subscriptions/{marketplaceSubscription}/operations/{operationId}";

            try
            {
                var operation = await _marketplaceRestClient.SendRequestAsync<string>(
                    new HttpMethod("PATCH"),
                    requestPath,
                    content: operationUpdate.ToJObject(),
                    cancellationToken: cancellationToken);

                var message = $"Subscription: {marketplaceSubscription}: Successfully updated operation for operation id: {operationId} with status {operationUpdate.Status}";
                _logger.Information(message);
                op.SetResultDescription(message);
            }
            catch (MarketplaceException ex)
            {
                var errorMessage = $"Subscription: {marketplaceSubscription}: Failed to update operation for operation id {operationId}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw new MarketplaceException(errorMessage, ex);
            }
        }

        public async Task DeleteSubscriptionAsync(
            MarketplaceSubscription marketplaceSubscription,
            CancellationToken cancellationToken = default)
        {
            using var op = _logger.StartTimedOperation(nameof(DeleteSubscriptionAsync));
            var requestPath = $"/api/saas/subscriptions/{marketplaceSubscription}";

            try
            {
                var operation = await _marketplaceRestClient.SendRequestWithPollingAsync<SubscriptionOperation>(
                    HttpMethod.Delete,
                    requestPath,
                    cancellationToken: cancellationToken);

                var message = $"Subscription: {marketplaceSubscription}: Successfully deleted subscription {marketplaceSubscription}";
                _logger.Information(message);
                op.SetResultDescription(message);
            }
            catch (MarketplaceException marketplaceException)
            {
                var errorMessage = $"Subscription: {marketplaceSubscription}: Failed to delete subscription {marketplaceSubscription}";
                _logger.Error(marketplaceException, errorMessage);
                op.FailOperation(errorMessage);
                throw new MarketplaceException(errorMessage, marketplaceException);
            }
        }
    }
}
