//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.IdempotentRPWorker.Constants;
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.IdempotentRPWorker.Utils;
using Microsoft.Liftr.Marketplace;
using Microsoft.Liftr.Marketplace.Agreement.Interfaces;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.ARM.Models;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;
using Microsoft.Liftr.Polly;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.IdempotentRPWorker.MarketplaceStates
{
    public class CreateSaaS : IState<StatesEnum, StateContext>
    {
        private readonly IMarketplaceARMClient _marketplaceARMClient;
        private readonly IMarketplaceFulfillmentClient _marketplaceFulfillmentClient;
        private readonly ISignAgreementService _signAgreementService;
        private readonly ILogger _logger;
        private readonly SaaSClientHack _saaSClientHack;

        public CreateSaaS(IMarketplaceARMClient marketplaceARMClient, IMarketplaceFulfillmentClient marketplaceFulfillmentClient, ISignAgreementService signAgreementService, ILogger logger, SaaSClientHack saaSClientHack)
        {
            _marketplaceARMClient = marketplaceARMClient ?? throw new ArgumentNullException(nameof(marketplaceARMClient));
            _marketplaceFulfillmentClient = marketplaceFulfillmentClient ?? throw new ArgumentNullException(nameof(marketplaceFulfillmentClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _saaSClientHack = saaSClientHack ?? throw new ArgumentNullException(nameof(saaSClientHack));
            _signAgreementService = signAgreementService ?? throw new ArgumentNullException(nameof(signAgreementService));
        }

        public StatesEnum State { get; set; } = StatesEnum.CreateSaaS;

        public bool IsSucessfullyExecuted { get; set; }

        public bool IsRetryable { get; set; }

        public async Task<StateContext> ExecuteAsync(StateContext stateContext, BaseResource resource)
        {
            var _loggerPrefix = $"[{nameof(CreateSaaS)}]";

            var marketplaceOfferDetail = MarketplaceHelper.GetMarketplaceOfferDetail(resource, stateContext);

            // checking object Id for CSP
            if (string.IsNullOrWhiteSpace(stateContext.RequestMetadata?.MarketplaceMetadata?.MSClientObjectId))
            {
                _logger.Information($"{_loggerPrefix} Object Id in Marketplace Request MetaData is null or empty for the ResourceId: {resource.Id} Azure Subscription: {stateContext.SubscriptionId} and TenantId: {stateContext.TenantId}. It could be a CSP Subscription!!");
            }

            // checking Client group membership for CSP purchase
            if (!string.IsNullOrWhiteSpace(stateContext.RequestMetadata?.MarketplaceMetadata?.MSClientGroupMembership))
            {
                _logger.Information($"{_loggerPrefix} Client Group Membership Header is present for purchase for the ResourceId: {resource.Id} Azure Subscription: {stateContext.SubscriptionId} and TenantId: {stateContext.TenantId}. It could be a CSP Subscription!!");
            }

            if (_saaSClientHack.ShouldIgnoreSaaSCreateFailure(stateContext.SubscriptionId))
            {
                _logger.Information($"{_loggerPrefix} Skipping Saas resource creation for ignored subscription {stateContext.SubscriptionId}");
                MarketplaceHelper.UpdateMarketplaceStateContext(stateContext, null);
                return stateContext;
            }

            _logger.Information($"{_loggerPrefix} Creating Marketplace SaaS resource on Azure subscription {stateContext.SubscriptionId} for Confluent resource: {@resource.Id} with plan id {marketplaceOfferDetail.PlanId} and offer id {marketplaceOfferDetail.OfferId} and billing term {marketplaceOfferDetail.TermId} and publisher id {marketplaceOfferDetail.PublisherId}");
            var saasResourceDetails = await CreateSAASResourceAsync(marketplaceOfferDetail, stateContext.RequestMetadata.MarketplaceMetadata, resource, stateContext.TenantId, stateContext.ApiVersion, isSubscriptionLevel: true);
            MarketplaceHelper.UpdateMarketplaceStateContext(stateContext, saasResourceDetails);
            return stateContext;
        }

        public async Task<StateContext> RollbackAsync(StateContext stateContext, BaseResource resource)
        {
            resource = resource ?? throw new ArgumentNullException(nameof(resource));
            stateContext = stateContext ?? throw new ArgumentNullException(nameof(stateContext));
            if (_saaSClientHack.ShouldIgnoreSaaSCreateFailure(stateContext.SubscriptionId))
            {
                _logger.Information($"[{nameof(CreateSaaS)}] [{MPConstants.SAASLogTag}] Skipping Saas deletion for ignored subscription {stateContext.SubscriptionId}");
                return stateContext;
            }

            if (stateContext.IsSaaSDeleted)
            {
                _logger.Information($"[{nameof(CreateSaaS)}] [{MPConstants.SAASLogTag}] SaaS resource is already deleted for Marketplace Subscription Id {stateContext.MarketplaceContext.MarketplaceSubscription}");
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
                errorMessage = $"[{nameof(CreateSaaS)}] [{MPConstants.SAASLogTag}] Deletion of marketplace SAAS Resource {stateContext.MarketplaceContext.MarketplaceSubscription} Failed. Error: {ex.Message}";
                throw;
            }
            catch (Exception ex)
            {
                errorMessage = $"[{nameof(CreateSaaS)}] [{MPConstants.SAASLogTag}] Deletion of marketplace SAAS Resource {stateContext.MarketplaceContext.MarketplaceSubscription} Failed. Error: {ex.Message}";
                throw;
            }

            return stateContext;
        }

        private async Task<MarketplaceSubscriptionDetails> CreateSAASResourceAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata, BaseResource resource, string tenantId, string apiVersion, bool isSubscriptionLevel)
        {
            string errorMessage = string.Empty;
            MarketplaceSubscriptionDetails marketplaceSubscriptionDetails = null;
            HttpResponseMessage response = null;
            try
            {
                var retryPolicy = HttpRetryPolicy.GetDefaultMarketplaceRetryPolicy(_logger);

                if (isSubscriptionLevel)
                {
                    var resourceDetails = new ResourceId(resource.Id);
                    var resourceGroup = resourceDetails.ResourceGroup;
                    _logger.Information($"Getting and Signing Agreement before creating SaaS Resource at subscription level for resource: {resource.Id} and resourceGroup: {resourceGroup}");
                    var agreementResponse = await _signAgreementService.GetandSignAgreementUsingTokenServiceAsync(saasResourceProperties, requestMetadata);

                    _logger.Information($"Fetched Agreement response with Id:{agreementResponse?.Id} and Creating Subscription Level SaaS for resource: {resource.Id} and resourceGroup: {resourceGroup}");
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        (marketplaceSubscriptionDetails, response) = await GetCreateSaaSResourceResponseAsync(saasResourceProperties, requestMetadata, resourceGroup);
                        return response;
                    });
                }
                else
                {
                    _logger.Information($"Creating Tenant Level SaaS for resource: {resource.Id}");
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        (marketplaceSubscriptionDetails, response) = await GetCreateSaaSResourceResponseAsync(saasResourceProperties, requestMetadata);
                        return response;
                    });
                }

                return marketplaceSubscriptionDetails;
            }
            catch (MarketplaceException mex)
            {
                errorMessage = $"[{nameof(CreateSAASResourceAsync)}] [{MPConstants.SAASLogTag}] Failed to Create saas resource. Error: {mex.Message}";
                throw;
            }
            catch (Exception ex)
            {
                errorMessage = $"[{nameof(CreateSAASResourceAsync)}] [{MPConstants.SAASLogTag}] Failed to Create saas resource. Error: {ex.Message}";
                throw;
            }
        }

        private async Task<(MarketplaceSubscriptionDetails, HttpResponseMessage)> GetCreateSaaSResourceResponseAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata, string resourceGroup = null)
        {
            HttpResponseMessage response = null;
            MarketplaceSubscriptionDetails marketplaceSubscriptionDetails = null;
            try
            {
                if (resourceGroup != null)
                {
                    marketplaceSubscriptionDetails = await _marketplaceARMClient.CreateSaaSResourceAsync(saasResourceProperties, requestMetadata, resourceGroup);
                }
                else
                {
                    marketplaceSubscriptionDetails = await _marketplaceARMClient.CreateSaaSResourceAsync(saasResourceProperties, requestMetadata);
                }

                response = new HttpResponseMessage()
                {
                    Content = new StringContent(HttpStatusCode.OK.ToString()),
                    StatusCode = HttpStatusCode.OK,
                };
            }
            catch (RequestFailedException ex)
            {
                _logger.Error(ex, $"Error occured while creating SaaS resource. Exception: {ex.Message} and Status code: {ex.Response?.StatusCode}");
                response = new HttpResponseMessage()
                {
                    Content = new StringContent(ex.Message),
                    StatusCode = ex.Response.StatusCode,
                };
            }

            return (marketplaceSubscriptionDetails, response);
        }
    }
}
