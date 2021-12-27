//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.IdempotentRPWorker.Constants;
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.ARM.Models;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.IdempotentRPWorker.Utils
{
    public static class MarketplaceHelper
    {
        public static MarketplaceSaasResourceProperties GetMarketplaceOfferDetail(BaseResource resource, StateContext stateContext)
        {
            resource = resource ?? throw new ArgumentNullException(nameof(resource));
            stateContext = stateContext ?? throw new ArgumentNullException(nameof(stateContext));
            return new MarketplaceSaasResourceProperties
            {
                PublisherId = stateContext.MarketplaceContext.PublisherId, /*"isvtestuklegacy"*/
                OfferId = stateContext.MarketplaceContext.OfferId, // "liftr_cf_dev"
                Name = $"{resource.Name}-{stateContext.WorkerRetryCount}", // SaaS name with workerRetryCount
                PlanId = stateContext.MarketplaceContext.PlanId,  // "payg"
                PaymentChannelType = stateContext.MarketplaceContext.PaymentChannelType,
                TermId = GetMarketplaceTermId(stateContext),
                Quantity = 1,
                PaymentChannelMetadata = new PaymentChannelMetadata
                {
                    AzureSubscriptionId = stateContext.SubscriptionId, /*For testing purpose using Liftr Marketplace Testing - PAYG subs*/
                },
            };
        }

        public static BillingTermTypes GetMarketplaceBillingTermType(StateContext stateContext)
        {
            stateContext = stateContext ?? throw new ArgumentNullException(nameof(stateContext));
            return stateContext.MarketplaceContext.TermUnit.Equals(MPConstants.MonthlyBillingTermType, StringComparison.Ordinal) ? BillingTermTypes.Monthly : BillingTermTypes.Yearly;
        }

        public static string GetMarketplaceTermId(StateContext stateContext)
        {
            stateContext = stateContext ?? throw new ArgumentNullException(nameof(stateContext));

            BillingTermTypes billingTerm = GetMarketplaceBillingTermType(stateContext);
            string termId = MPConstants.MonthlyOfferTermId;
            if (billingTerm == BillingTermTypes.Yearly)
            {
                termId = MPConstants.YearlyOfferTermId;
            }

            return termId;
        }

        public static void UpdateMarketplaceStateContext(StateContext stateContext, MarketplaceSubscriptionDetails saasResourceDetails)
        {
            bool isFakeSaaS = false;
            stateContext = stateContext ?? throw new ArgumentNullException(nameof(stateContext));
            isFakeSaaS = saasResourceDetails == null ? true : false;

            stateContext.MarketplaceContext.MarketplaceSubscription = isFakeSaaS ? new MarketplaceSubscription(Guid.NewGuid()) : new MarketplaceSubscription(Guid.ParseExact(saasResourceDetails?.Id, "D"));
            stateContext.MarketplaceContext.Name = isFakeSaaS ? MPConstants.FakeSaaSResource : saasResourceDetails?.Name;
            stateContext.MarketplaceContext.TermId = GetMarketplaceTermId(stateContext);
            stateContext.MarketplaceContext.TermType = GetMarketplaceBillingTermType(stateContext);
            stateContext.MarketplaceContext.IsSubscriptionLevel = isFakeSaaS ? false : saasResourceDetails?.AdditionalMetadata?.IsSubscriptionLevel;
        }

        public static async Task<HttpResponseMessage> GetDeleteSaaSResourceResponseAsync(string subscriptionId, string resourceName, string resourceGroup, MarketplaceRequestMetadata requestMetadata, IMarketplaceARMClient _marketplaceARMClient, ILogger _logger)
        {
            _marketplaceARMClient = _marketplaceARMClient ?? throw new ArgumentNullException(nameof(_marketplaceARMClient));
            _logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
            HttpResponseMessage response = null;
            try
            {
                await _marketplaceARMClient.DeleteSaaSResourceAsync(subscriptionId, resourceName, resourceGroup, requestMetadata);
                response = new HttpResponseMessage()
                {
                    Content = new StringContent(HttpStatusCode.OK.ToString()),
                    StatusCode = HttpStatusCode.OK,
                };
            }
            catch (RequestFailedException ex)
            {
                _logger.Error(ex, $"Error occured while deleting SaaS resource. {ex.Message}");
                response = new HttpResponseMessage()
                {
                    Content = new StringContent(ex.Message),
                    StatusCode = ex.Response.StatusCode,
                };
            }

            return response;
        }

        public static async Task<HttpResponseMessage> GetDeleteSubscriptionResponseAsync(MarketplaceSubscription marketplaceSubscription, IMarketplaceFulfillmentClient _marketplaceFulfillmentClient, ILogger _logger, CancellationToken token = default)
        {
            _marketplaceFulfillmentClient = _marketplaceFulfillmentClient ?? throw new ArgumentNullException(nameof(_marketplaceFulfillmentClient));
            _logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
            HttpResponseMessage response = null;
            try
            {
                await _marketplaceFulfillmentClient.DeleteSubscriptionAsync(marketplaceSubscription, token);
                response = new HttpResponseMessage()
                {
                    Content = new StringContent(HttpStatusCode.OK.ToString()),
                    StatusCode = HttpStatusCode.OK,
                };
            }
            catch (RequestFailedException ex)
            {
                _logger.Error(ex, $"Error occured while deleting SaaS resource. {ex.Message}");
                response = new HttpResponseMessage()
                {
                    Content = new StringContent(ex.Message),
                    StatusCode = ex.Response.StatusCode,
                };
            }

            return response;
        }
    }
}
