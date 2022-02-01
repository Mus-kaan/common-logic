//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.IdempotentRPWorker.Constants;
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.Utils;
using Microsoft.Liftr.Marketplace;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;
using Microsoft.Liftr.Marketplace.Saas.Models;
using Microsoft.Liftr.Polly;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.IdempotentRPWorker.MarketplaceStates
{
    public class ActivateSaaS : IState<StatesEnum, StateContext>
    {
        private readonly IMarketplaceFulfillmentClient _marketplaceFulfillmentClient;
        private readonly IMarketplaceARMClient _marketplaceARMClient;
        private readonly ILogger _logger;
        private readonly SaaSClientHack _saaSClientHack;

        public ActivateSaaS(IMarketplaceFulfillmentClient marketplaceFulfillmentClient, IMarketplaceARMClient marketplaceARMClient, ILogger logger, SaaSClientHack saaSClientHack)
        {
            _marketplaceARMClient = marketplaceARMClient ?? throw new ArgumentNullException(nameof(marketplaceARMClient));
            _marketplaceFulfillmentClient = marketplaceFulfillmentClient ?? throw new ArgumentNullException(nameof(marketplaceFulfillmentClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _saaSClientHack = saaSClientHack ?? throw new ArgumentNullException(nameof(saaSClientHack));
        }

        public StatesEnum State { get; set; } = StatesEnum.ActivateSaaS;

        public bool IsSucessfullyExecuted { get; set; }

        public bool IsRetryable { get; set; }

        public async Task<StateContext> ExecuteAsync(StateContext stateContext, BaseResource resource)
        {
            resource = resource ?? throw new ArgumentNullException(nameof(resource));
            stateContext = stateContext ?? throw new ArgumentNullException(nameof(stateContext));
            if (_saaSClientHack.ShouldIgnoreSaaSCreateFailure(stateContext.SubscriptionId))
            {
                _logger.Information($"[{nameof(ActivateSaaS)}] [{MPConstants.SAASLogTag}] Skipping Saas activation for ignored subscription {stateContext.SubscriptionId}");
                return stateContext;
            }

            using var op = _logger.StartTimedOperation($"[{nameof(ActivateSaaS)}] [{MPConstants.SAASLogTag}] Activate Confluent Subscription");
            var resourceId = resource.Id;

            _logger.Information(
                $"[{nameof(ActivateSaaS)}] [{MPConstants.SAASLogTag}] Activating Marketplace Subscription: {stateContext.SubscriptionId} for resource: {@resourceId}",
                stateContext.MarketplaceContext.MarketplaceSubscription,
                resourceId);

            await ActivateMarketplaceSAASResourceAsync(stateContext, resource);

            stateContext.SetSaaSActivationStatus(true);

            _logger.Information(
                $"[{nameof(ActivateSaaS)}] [{MPConstants.SAASLogTag}] Marketplace Subscription: {stateContext.SubscriptionId} successfully activated for resource: {stateContext.ResourceId}",
                stateContext.MarketplaceContext.MarketplaceSubscription,
                resourceId);
            op.SetResultDescription($"[{MPConstants.SAASLogTag}] Successfully activated the Marketplace Subscription: {stateContext.MarketplaceContext.MarketplaceSubscription}");

            return stateContext;
        }

        public async Task<StateContext> RollbackAsync(StateContext stateContext, BaseResource resource)
        {
            resource = resource ?? throw new ArgumentNullException(nameof(resource));
            stateContext = stateContext ?? throw new ArgumentNullException(nameof(stateContext));
            if (_saaSClientHack.ShouldIgnoreSaaSCreateFailure(stateContext.SubscriptionId))
            {
                _logger.Information($"[{nameof(ActivateSaaS)}] [{MPConstants.SAASLogTag}] Skipping Saas deletion for ignored subscription {stateContext.SubscriptionId}");
                return stateContext;
            }

            if (stateContext.IsSaaSDeleted)
            {
                _logger.Information($"[{nameof(ActivateSaaS)}] [{MPConstants.SAASLogTag}] SaaS resource is already deleted for Marketplace Subscription Id {stateContext.MarketplaceContext.MarketplaceSubscription}");
                return stateContext;
            }

            _logger.Information(
                $"[{nameof(DeleteSaaS)}] [{MPConstants.SAASLogTag}] Deleting Marketplace subscription: {stateContext.SubscriptionId} for resource: {resource.Id}",
                stateContext.MarketplaceContext.MarketplaceSubscription,
                resource.Id,
                stateContext.TenantId);

            string errorMessage = string.Empty;

            try
            {
                var retryPolicy = HttpRetryPolicy.GetDefaultMarketplaceRetryPolicy(_logger);
                bool isSubsLevel = (bool)stateContext.MarketplaceContext.IsSubscriptionLevel;
                if (isSubsLevel)
                {
                    var resourceName = stateContext.MarketplaceContext.Name;
                    var resourceDetails = new ResourceId(resource.Id);
                    var resourceGroup = resourceDetails.ResourceGroup;
                    _logger.Information($"Deleting Subscription level SaaS resource: {resourceName}, resourceId: {resource.Id}, resourceGroup: {resourceGroup}");
                    var response = await retryPolicy.ExecuteAsync(async () => await MarketplaceHelper.GetDeleteSaaSResourceResponseAsync(stateContext.SubscriptionId, resourceName, resourceGroup, stateContext.RequestMetadata.MarketplaceMetadata, _marketplaceARMClient, _logger));
                }
                else
                {
                    _logger.Information($"Deleting SaaS resource using Delete Fulfillment Call. resourceId: {resource.Id}, MarketplaceSubscription: {stateContext.MarketplaceContext.MarketplaceSubscription.Id}");
                    var response = await retryPolicy.ExecuteAsync(async () => await MarketplaceHelper.GetDeleteSubscriptionResponseAsync(stateContext.MarketplaceContext.MarketplaceSubscription, _marketplaceFulfillmentClient, _logger));
                }

                _logger.Information(
                $"[{nameof(DeleteSaaS)}] [{MPConstants.SAASLogTag}] Marketplace SaaS resource: {stateContext.MarketplaceContext.MarketplaceSubscription} successfully deleted for resource: {resource.Id}",
                stateContext.MarketplaceContext.MarketplaceSubscription,
                resource.Id,
                stateContext.TenantId);

                stateContext.SetSaaSDeleteStatus(true);
            }
            catch (MarketplaceException ex)
            {
                errorMessage = $"[{nameof(ActivateSaaS)}] [{MPConstants.SAASLogTag}] Deletion of marketplace SAAS Resource {stateContext.MarketplaceContext.MarketplaceSubscription} Failed. Error: {ex.Message}";
                throw;
            }
            catch (Exception ex)
            {
                errorMessage = $"[{nameof(ActivateSaaS)}] [{MPConstants.SAASLogTag}] Deletion of marketplace SAAS Resource {stateContext.MarketplaceContext.MarketplaceSubscription} Failed. Error: {ex.Message}";
                throw;
            }

            return stateContext;
        }

        private async Task ActivateMarketplaceSAASResourceAsync(StateContext stateContext, BaseResource resource, CancellationToken token = default)
        {
            string errorMessage = string.Empty;
            try
            {
                var retryPolicy = HttpRetryPolicy.GetMarketplaceRetryPolicyForEntityNotFound(_logger);
                var response = await retryPolicy.ExecuteAsync(async () => await GetActivateSaaSSubscriptionResponseAsync(stateContext.MarketplaceContext.MarketplaceSubscription, stateContext.MarketplaceContext.PlanId, stateContext.MarketplaceContext.Quantity, token));
            }
            catch (MarketplaceException mex)
            {
                errorMessage = $"[{nameof(ActivateMarketplaceSAASResourceAsync)}] [{MPConstants.SAASLogTag}] Failed to Activate saas resource. Error: {mex.Message}";
                throw;
            }
            catch (Exception ex)
            {
                errorMessage = $"[{nameof(ActivateMarketplaceSAASResourceAsync)}] [{MPConstants.SAASLogTag}] Failed to Activate saas resource. Error: {ex.Message}";
                throw;
            }
        }

        private async Task<HttpResponseMessage> GetActivateSaaSSubscriptionResponseAsync(MarketplaceSubscription marketplaceSubscription, string plan, int? quantity, CancellationToken token = default)
        {
            HttpResponseMessage response = null;
            try
            {
                await _marketplaceFulfillmentClient.ActivateSaaSSubscriptionAsync(
                                        new ActivateSubscriptionRequest(
                                            marketplaceSubscription,
                                            plan,
                                            quantity),
                                        token);
                response = new HttpResponseMessage()
                {
                    Content = new StringContent(HttpStatusCode.OK.ToString()),
                    StatusCode = HttpStatusCode.OK,
                };
            }
            catch (RequestFailedException ex)
            {
                _logger.Error(ex, $"Error occured while activating SaaS subscription. {ex.Message}");
                response = new HttpResponseMessage()
                {
                    Content = ex.Response.Content,
                    StatusCode = ex.Response.StatusCode,
                };
                if (ex.Response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string responseContentAsString = await response.Content.ReadAsStringAsync();
                    bool isEntityNotFound = responseContentAsString.OrdinalContains(MPConstants.EntityNotFound);

                    // Retrying only when status code is BadRequest and content contains EntityNotFound.
                    if (isEntityNotFound)
                    {
                        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(MPConstants.MarketplaceRetryWaitForEntityNotFound));
                        _logger.Error(ex, $"SaaS Resource with subscription {marketplaceSubscription} not found!!!");
                    }
                    else
                    {
                        // Setting status code to OK just to skip retries.
                        response.StatusCode = HttpStatusCode.OK;
                    }
                }
            }

            return response;
        }
    }
}
