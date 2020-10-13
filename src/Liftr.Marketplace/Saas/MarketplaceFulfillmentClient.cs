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
            var requestPath = MarketplaceUrlHelper.GetRequestPath(MarketplaceEnum.ResolveToken);

            try
            {
                var response = await _marketplaceRestClient.SendRequestAsync<ResolvedMarketplaceSubscription>(
                HttpMethod.Post,
                requestPath,
                new Dictionary<string, string>() { { "x-ms-marketplace-token", marketplaceToken } },
                cancellationToken: cancellationToken);

                _logger.Information("Subscription: {@resolvedSubscription} resolved", response);
                op.SetResultDescription($"Subscription: {response} resolved");
                return response;
            }
            catch (MarketplaceHttpException ex)
            {
                var errorMessage = "Failed to resolve subscription";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw new MarketplaceException(errorMessage, ex);
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
            var requestPath = MarketplaceUrlHelper.GetRequestPath(MarketplaceEnum.ActivateSubscription, activateSubscriptionRequest.MarketplaceSubscription);

            try
            {
                await _marketplaceRestClient.SendRequestAsync<string>(
                HttpMethod.Post,
                requestPath,
                content: activateSubscriptionRequest.ToJObject(),
                cancellationToken: token);

                _logger.Information($"SAAS Subscription: {activateSubscriptionRequest.MarketplaceSubscription} activated");
                op.SetResultDescription($"Subscription: {activateSubscriptionRequest.MarketplaceSubscription} activated");
            }
            catch (MarketplaceHttpException ex)
            {
                var errorMessage = $"Failed to activate SAAS subscription: {activateSubscriptionRequest.MarketplaceSubscription} Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation($"Subscription: {activateSubscriptionRequest.MarketplaceSubscription} could not be activated");
                throw new MarketplaceException(errorMessage, ex);
            }
        }

        public async Task<SubscriptionOperation> GetOperationAsync(
            MarketplaceSubscription marketplaceSubscription,
            Guid operationId,
            CancellationToken cancellationToken = default)
        {
            using var op = _logger.StartTimedOperation(nameof(GetOperationAsync));
            var requestPath = MarketplaceUrlHelper.GetRequestPath(MarketplaceEnum.GetOperation, marketplaceSubscription, operationId);

            try
            {
                var operation = await _marketplaceRestClient.SendRequestAsync<SubscriptionOperation>(
                    HttpMethod.Get,
                    requestPath,
                    cancellationToken: cancellationToken);

                var message = $"SAAS Subscription: {marketplaceSubscription}: Successfully retrieved operation to authorize Webhook notification for operation id: {operationId} with status {operation.Status}";
                _logger.Information(message);
                op.SetResultDescription(message);
                return operation;
            }
            catch (MarketplaceHttpException ex)
            {
                var errorMessage = $"SAAS Subscription: {marketplaceSubscription}: Failed to get operation to authorize Webhook notification for operation id {operationId} Error: {ex.Message}";
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
            var requestPath = MarketplaceUrlHelper.GetRequestPath(MarketplaceEnum.ListOperations, marketplaceSubscription);

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
            catch (MarketplaceHttpException ex)
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
            var requestPath = MarketplaceUrlHelper.GetRequestPath(MarketplaceEnum.UpdateOperation, marketplaceSubscription, operationId);

            try
            {
                var operation = await _marketplaceRestClient.SendRequestAsync<string>(
                    new HttpMethod("PATCH"),
                    requestPath,
                    content: operationUpdate.ToJObject(),
                    cancellationToken: cancellationToken);

                var message = $"SAAS Subscription: {marketplaceSubscription}: Successfully updated operation to Marketplace for Webhook notification for operation id: {operationId} with status {operationUpdate.Status}";
                _logger.Information(message);
                op.SetResultDescription(message);
            }
            catch (MarketplaceHttpException ex)
            {
                var errorMessage = $"SAAS Subscription: {marketplaceSubscription}: Failed to update operation to Marketplace for Webhook notification for operation id {operationId} Error: {ex.Message}";
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
            var requestPath = MarketplaceUrlHelper.GetRequestPath(MarketplaceEnum.DeleteSubscription, marketplaceSubscription);

            try
            {
                var operation = await _marketplaceRestClient.SendRequestWithPollingAsync<SubscriptionOperation>(
                    HttpMethod.Delete,
                    requestPath,
                    cancellationToken: cancellationToken);

                var message = $"SAAS Subscription: {marketplaceSubscription}: Successfully deleted subscription {marketplaceSubscription}";
                _logger.Information(message);
                op.SetResultDescription(message);
            }
            catch (MarketplaceHttpException marketplaceException)
            {
                var errorMessage = $"SAAS Subscription: {marketplaceSubscription}: Failed to delete subscription {marketplaceSubscription} Error: {marketplaceException.Message}";
                _logger.Error(marketplaceException, errorMessage);
                op.FailOperation(errorMessage);
                throw new MarketplaceException(errorMessage, marketplaceException);
            }
        }

        public async Task<MarketplaceSubscriptionDetails> GetSubscriptionAsync(
            MarketplaceSubscription marketplaceSubscription,
            CancellationToken cancellationToken = default)
        {
            if (marketplaceSubscription is null)
            {
                throw new ArgumentNullException(nameof(marketplaceSubscription));
            }

            using var op = _logger.StartTimedOperation(nameof(GetSubscriptionAsync));
            op.SetContextProperty(nameof(marketplaceSubscription), marketplaceSubscription.ToString());
            var requestPath = MarketplaceUrlHelper.GetRequestPath(MarketplaceEnum.GetSubscription, marketplaceSubscription);

            try
            {
                var subscriptionDetails = await _marketplaceRestClient.SendRequestAsync<MarketplaceSubscriptionDetails>(
                    HttpMethod.Get,
                    requestPath,
                    cancellationToken: cancellationToken);

                var message = $"Successfully obtained subscription {marketplaceSubscription} with name {subscriptionDetails.Name}";
                _logger.Information(message);
                op.SetResultDescription(message);
                return subscriptionDetails;
            }
            catch (MarketplaceHttpException marketplaceException)
            {
                var errorMessage = $"Failed to get subscription";
                _logger.Error(marketplaceException, errorMessage);
                op.FailOperation(errorMessage);
                throw new MarketplaceException(errorMessage, marketplaceException);
            }
        }
    }
}
