//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.Constants;
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.Utils;
using Microsoft.Liftr.Marketplace;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;
using Microsoft.Liftr.Polly;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.IdempotentRPWorker.MarketplaceStates
{
    public class DeleteSaaS : IState<StatesEnum, StateContext>
    {
        private readonly IMarketplaceFulfillmentClient _marketplaceFulfillmentClient;
        private readonly IMarketplaceARMClient _marketplaceARMClient;
        private readonly ILogger _logger;
        private readonly SaaSClientHack _saaSClientHack;

        public DeleteSaaS(IMarketplaceFulfillmentClient marketplaceFulfillmentClient, IMarketplaceARMClient marketplaceARMClient, ILogger logger, SaaSClientHack saaSClientHack)
        {
            _marketplaceFulfillmentClient = marketplaceFulfillmentClient ?? throw new ArgumentNullException(nameof(marketplaceFulfillmentClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _saaSClientHack = saaSClientHack ?? throw new ArgumentNullException(nameof(saaSClientHack));
            _marketplaceARMClient = marketplaceARMClient ?? throw new ArgumentNullException(nameof(marketplaceARMClient));
        }

        public StatesEnum State { get; set; } = StatesEnum.DeleteSaaS;

        public bool IsSucessfullyExecuted { get; set; }

        public bool IsRetryable { get; set; }

        public async Task<StateContext> ExecuteAsync(StateContext stateContext, BaseResource resource)
        {
            resource = resource ?? throw new ArgumentNullException(nameof(resource));
            stateContext = stateContext ?? throw new ArgumentNullException(nameof(stateContext));
            if (_saaSClientHack.ShouldIgnoreSaaSCreateFailure(stateContext.SubscriptionId))
            {
                _logger.Information($"[{nameof(DeleteSaaS)}] [{MPConstants.SAASLogTag}] Skipping Saas deletion for ignored subscription {stateContext.SubscriptionId}");
                return stateContext;
            }

            if (stateContext.IsSaaSDeleted)
            {
                _logger.Information($"[{nameof(DeleteSaaS)}] [{MPConstants.SAASLogTag}] SaaS resource is already deleted for Marketplace Subscription Id {stateContext.MarketplaceContext.MarketplaceSubscription}");
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
                errorMessage = $"[{nameof(DeleteSaaS)}] [{MPConstants.SAASLogTag}] Deletion of marketplace SAAS Resource {stateContext.MarketplaceContext.MarketplaceSubscription} Failed. Error: {ex.Message}";
                throw;
            }
            catch (Exception ex)
            {
                errorMessage = $"[{nameof(DeleteSaaS)}] [{MPConstants.SAASLogTag}] Deletion of marketplace SAAS Resource {stateContext.MarketplaceContext.MarketplaceSubscription} Failed. Error: {ex.Message}";
                throw;
            }

            return stateContext;
        }

        public async Task<StateContext> RollbackAsync(StateContext stateContext, BaseResource resource)
        {
            await Task.CompletedTask;
            return stateContext;
        }
    }
}
