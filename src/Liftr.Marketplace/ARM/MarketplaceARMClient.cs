//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.ARM.Models;
using Microsoft.Liftr.Marketplace.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.ARM
{
#nullable enable
    /// <inheritdoc/>
    public sealed class MarketplaceARMClient : IMarketplaceARMClient
    {
        private const string ResourceTypePath = "api/saasresources/subscriptions";
        private const string MarketplaceLiftrStoreFront = "StoreForLiftr";
        private readonly ILogger _logger;
        private readonly MarketplaceRestClient _marketplaceRestClient;

        public MarketplaceARMClient(
            ILogger logger,
            MarketplaceRestClient marketplaceRestClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _marketplaceRestClient = marketplaceRestClient ?? throw new ArgumentNullException(nameof(marketplaceRestClient));
        }

        public async Task<MarketplaceSubscriptionDetails> CreateSaaSResourceAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata)
        {
            if (saasResourceProperties is null || !saasResourceProperties.IsValid())
            {
                throw new ArgumentNullException(nameof(saasResourceProperties), $"Please provide valid {nameof(MarketplaceSaasResourceProperties)}");
            }

            if (requestMetadata is null || !requestMetadata.IsValid())
            {
                throw new ArgumentNullException(nameof(requestMetadata), $"Please provide valid {nameof(MarketplaceRequestMetadata)}");
            }

            using var op = _logger.StartTimedOperation(nameof(CreateSaaSResourceAsync));
            try
            {
                var additionalHeaders = GetAdditionalMarketplaceHeaders(requestMetadata);
                var json = saasResourceProperties.ToJObject();
                var createdResource = await _marketplaceRestClient.SendRequestWithPollingAsync<SaasCreationResponse>(HttpMethod.Put, ResourceTypePath, additionalHeaders, json);
                _logger.Information("Marketplace resource has been successfully created. {@resource}", createdResource);
                return createdResource.SubscriptionDetails;
            }
            catch (MarketplaceHttpException ex)
            {
                string errorMessage = $"Failed to create marketplace saas resource while making create request. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw new MarketplaceException(errorMessage, ex);
            }
        }

        public async Task<MarketplaceSaasTokenResponse> GetAccessTokenAsync(string resourceId)
        {
            var resourcePath = ResourceTypePath + "/" + resourceId + "/generateToken";

            try
            {
                var response = await _marketplaceRestClient.SendRequestAsync<MarketplaceSaasTokenResponse>(HttpMethod.Post, resourcePath);
                return response;
            }
            catch (MarketplaceHttpException ex)
            {
                var errorMessage = $"Failed to get access token for saas resource {resourceId}. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                throw;
            }
        }

        public async Task DeleteSaaSResourceAsync(MarketplaceSubscription marketplaceSubscription, MarketplaceRequestMetadata requestMetadata)
        {
            if (marketplaceSubscription is null)
            {
                throw new ArgumentNullException(nameof(marketplaceSubscription));
            }

            if (requestMetadata is null || !requestMetadata.IsValid())
            {
                throw new ArgumentNullException(nameof(requestMetadata), $"Please provide valid {nameof(MarketplaceRequestMetadata)}");
            }

            var resourcePath = ResourceTypePath + "/" + marketplaceSubscription.Id.ToString();

            using var op = _logger.StartTimedOperation(nameof(DeleteSaaSResourceAsync));
            op.SetContextProperty(nameof(MarketplaceSubscription), marketplaceSubscription.ToString());

            try
            {
                var response = await _marketplaceRestClient.SendRequestAsync<string>(HttpMethod.Delete, resourcePath, GetAdditionalMarketplaceHeaders(requestMetadata));
                op.SetResultDescription($"Successfully deleted Marketplace subscription {marketplaceSubscription.ToString()}");
            }
            catch (MarketplaceHttpException ex)
            {
                string errorMessage = $"Failed to delete marketplace saas resource. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw new MarketplaceException(errorMessage, ex);
            }
        }

        private Dictionary<string, string> GetAdditionalMarketplaceHeaders(MarketplaceRequestMetadata requestHeaders)
        {
            var additionalHeaders = new Dictionary<string, string>();
            var correlationId = TelemetryContext.GetOrGenerateCorrelationId();

            additionalHeaders.Add("x-ms-correlation-id", correlationId);
            additionalHeaders.Add("x-ms-client-object-id", requestHeaders.MSClientObjectId);
            additionalHeaders.Add("x-ms-client-tenant-id", requestHeaders.MSClientTenantId);

            additionalHeaders.Add("x-ms-client-issuer", requestHeaders.MSClientIssuer);
            additionalHeaders.Add("x-ms-client-name", MarketplaceLiftrStoreFront);

            if (!string.IsNullOrEmpty(requestHeaders.MSClientPrincipalName))
            {
                additionalHeaders.Add("x-ms-client-principal-name", requestHeaders.MSClientPrincipalName);
            }

            if (!string.IsNullOrEmpty(requestHeaders.MSClientPrincipalId))
            {
                additionalHeaders.Add("x-ms-client-principal-id", requestHeaders.MSClientPrincipalId);
            }

            if (!string.IsNullOrEmpty(requestHeaders.MSClientAppId))
            {
                additionalHeaders.Add("x-ms-client-app-id", requestHeaders.MSClientAppId);
            }

            return additionalHeaders;
        }
    }
}
