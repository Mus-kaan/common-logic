//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.ARM.Models;
using Microsoft.Liftr.Marketplace.Contracts;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Utils;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.ARM
{
#nullable enable
    /// <inheritdoc/>
    public sealed class MarketplaceARMClient : IMarketplaceARMClient
    {
        private const string ResourceTypePath = "api/saasresources/subscriptions";
        private const string MigrateSaasPath = "/migrateFromTenant";
        private readonly ILogger _logger;
        private readonly MarketplaceRestClient _marketplaceRestClient;

        public MarketplaceARMClient(
            ILogger logger,
            MarketplaceRestClient marketplaceRestClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _marketplaceRestClient = marketplaceRestClient ?? throw new ArgumentNullException(nameof(marketplaceRestClient));
        }

        public MarketplaceARMClient(MarketplaceRestClient marketplaceRestClient)
            : this(LoggerFactory.ConsoleLogger, marketplaceRestClient)
        {
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
                var additionalHeaders = HttpRequestHelper.GetAdditionalMarketplaceHeaders(requestMetadata);
                var json = saasResourceProperties.ToJObject();
                var createdResource = await _marketplaceRestClient.SendRequestWithPollingAsync<BaseOperationResponse>(HttpMethod.Put, ResourceTypePath, additionalHeaders, json);
                var subscriptionDetails = createdResource.SubscriptionDetails;
                _logger.Information($"Marketplace SAAS resource has been successfully created. \n SAAS ResourceId: {subscriptionDetails.Id}, Name: {subscriptionDetails.Name}, Plan: {subscriptionDetails.PlanId}, Offer: {subscriptionDetails.OfferId}, Publisher: {subscriptionDetails.PublisherId}, SAAS Subscription Status: {subscriptionDetails.SaasSubscriptionStatus}, Azure Subscription: {subscriptionDetails.AdditionalMetadata?.AzureSubscriptionId}");
                return subscriptionDetails;
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"Failed to create marketplace SAAS resource while making create request. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw;
            }
        }

        public async Task<MarketplaceSubscriptionDetails> CreateSaaSResourceAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata, string resourceGroup)
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
                var subscriptionId = saasResourceProperties.PaymentChannelMetadata.AzureSubscriptionId;
                var resourceName = saasResourceProperties.Name;
                _logger.Information($"Subscription Level SAAS resource creation parameters: SubscriptionId: {subscriptionId}, ResourceGroup: {resourceGroup}, ResourceName: {resourceName}");

                var resourceTypePath = HttpRequestHelper.GetCompleteRequestPathForSubscriptionLevel(subscriptionId, resourceGroup, resourceName);
                _logger.Information($"Request Path at Subscription Level: {resourceTypePath}");
                var additionalHeaders = HttpRequestHelper.GetAdditionalMarketplaceHeaders(requestMetadata);
                var json = saasResourceProperties.ToJObject();
                var createdResource = await _marketplaceRestClient.SendRequestWithPollingAsync<BaseOperationResponse>(HttpMethod.Put, resourceTypePath, additionalHeaders, json);
                var subscriptionDetails = createdResource.SubscriptionDetails;

                // the following log message is being used in Common Telemetry System. Please inform the team at liftrcts@microsoft.com if you change the message or format
                _logger.Information($"Marketplace Subscription level SAAS resource has been successfully created. \n SAAS ResourceId: {subscriptionDetails.Id}, Name: {subscriptionDetails.Name}, Plan: {subscriptionDetails.PlanId}, Offer: {subscriptionDetails.OfferId}, Publisher: {subscriptionDetails.PublisherId}, SAAS Subscription Status: {subscriptionDetails.SaasSubscriptionStatus}, Azure Subscription: {subscriptionDetails.AdditionalMetadata?.AzureSubscriptionId}");
                return subscriptionDetails;
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"Failed to create subscription level marketplace SAAS resource while making create request. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw;
            }
        }

        public async Task<MarketplaceSubscriptionDetails> UpdateSaaSResourceAsync(
            string azureSubscriptionId,
            string resourceGroup,
            string resourceName,
            MarketplaceRequestMetadata requestMetadata,
            MarketplaceSaasResourceProperties updatedSaasResourceProperties)
        {
            if (string.IsNullOrWhiteSpace(azureSubscriptionId))
            {
                throw new ArgumentNullException(nameof(azureSubscriptionId), $"Please provide valid {nameof(azureSubscriptionId)}");
            }

            if (string.IsNullOrWhiteSpace(resourceGroup))
            {
                throw new ArgumentNullException(nameof(resourceGroup), $"Please provide valid {nameof(resourceGroup)}");
            }

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentNullException(nameof(resourceName), $"Please provide valid {nameof(resourceName)}");
            }

            if (updatedSaasResourceProperties is null)
            {
                throw new ArgumentNullException(nameof(updatedSaasResourceProperties), $"Please provide valid {nameof(MarketplaceSaasResourceProperties)}");
            }

            if (requestMetadata is null || !requestMetadata.IsValid())
            {
                throw new ArgumentNullException(nameof(requestMetadata), $"Please provide valid {nameof(MarketplaceRequestMetadata)}");
            }

            using var op = _logger.StartTimedOperation(nameof(CreateSaaSResourceAsync));
            try
            {
                _logger.Information($"Subscription Level SAAS resource update parameters: SubscriptionId: {azureSubscriptionId}, ResourceGroup: {resourceGroup}, ResourceName: {resourceName}");

                var resourceTypePath = HttpRequestHelper.GetCompleteRequestPathForSubscriptionLevel(azureSubscriptionId, resourceGroup, resourceName);
                _logger.Information($"Request Path at Subscription Level: {resourceTypePath}");
                var additionalHeaders = HttpRequestHelper.GetAdditionalMarketplaceHeaders(requestMetadata);
                var json = updatedSaasResourceProperties.ToJObject();
                var updatedResource = await _marketplaceRestClient.SendRequestWithPollingAsync<BaseOperationResponse>(new HttpMethod("PATCH"), resourceTypePath, additionalHeaders, json);
                var subscriptionDetails = updatedResource.SubscriptionDetails;

                // the following log message is being used in Common Telemetry System. Please inform the team at liftrcts@microsoft.com if you change the message or format
                _logger.Information($"Marketplace Subscription level SAAS resource has been successfully updated. \n SAAS ResourceId: {subscriptionDetails.Id}, Name: {subscriptionDetails.Name}, Plan: {subscriptionDetails.PlanId}, Offer: {subscriptionDetails.OfferId}, Publisher: {subscriptionDetails.PublisherId}, SAAS Subscription Status: {subscriptionDetails.SaasSubscriptionStatus}, Azure Subscription: {subscriptionDetails.AdditionalMetadata?.AzureSubscriptionId}");
                return subscriptionDetails;
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"Failed to update subscription level marketplace SAAS resource while making create request. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw;
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
            catch (RequestFailedException ex)
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
                var response = await _marketplaceRestClient.SendRequestAsync<string>(HttpMethod.Delete, resourcePath, HttpRequestHelper.GetAdditionalMarketplaceHeaders(requestMetadata));
                op.SetResultDescription($"Successfully deleted Marketplace subscription {marketplaceSubscription.ToString()}");
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"Failed to delete marketplace saas resource. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw;
            }
        }

        public async Task DeleteSaaSResourceAsync(string subscriptionId, string resourceName, string resourceGroup, MarketplaceRequestMetadata requestMetadata)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentNullException(nameof(resourceName));
            }

            if (string.IsNullOrWhiteSpace(resourceGroup))
            {
                throw new ArgumentNullException(nameof(resourceGroup));
            }

            if (requestMetadata is null || !requestMetadata.IsValid())
            {
                throw new ArgumentNullException(nameof(requestMetadata), $"Please provide valid {nameof(MarketplaceRequestMetadata)}");
            }

            _logger.Information($"Subscription level SAAS resource deletion parameters: SubscriptionId: {subscriptionId}, ResourceGroup: {resourceGroup}, ResourceName: {resourceName}");
            var resourceTypePath = HttpRequestHelper.GetCompleteRequestPathForSubscriptionLevel(subscriptionId, resourceGroup, resourceName);
            _logger.Information($"Path: {resourceTypePath}");
            var additionalHeaders = HttpRequestHelper.GetAdditionalMarketplaceHeaders(requestMetadata);

            using var op = _logger.StartTimedOperation(nameof(DeleteSaaSResourceAsync));
            op.SetContextProperty(nameof(MarketplaceSubscription), resourceTypePath);

            try
            {
                var response = await _marketplaceRestClient.SendRequestWithPollingAsync<BaseOperationResponse>(HttpMethod.Delete, resourceTypePath, additionalHeaders);
                op.SetResultDescription($"Successfully deleted Marketplace subscription level resource {resourceTypePath}");
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"Failed to delete marketplace susbcription level SAAS resource. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw;
            }
        }

        /// <summary>
        /// For Subscription level purchase eligibility check
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="mpCheckEligibilityRequest"></param>
        /// <param name="mpRequestMetadata"></param>
        /// <returns>Payment validation status</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<PaymentValidationResponse> ValidatesSaaSPurchasePaymentAsync(string resourceName, string resourceGroup, MPCheckEligibilityRequest mpCheckEligibilityRequest, MarketplaceRequestMetadata mpRequestMetadata)
        {
            ValidateSaaSPurchaseRequest(resourceName, resourceGroup, mpCheckEligibilityRequest, mpRequestMetadata);

            using var op = _logger.StartTimedOperation(nameof(ValidatesSaaSPurchasePaymentAsync));
            var azSubscriptionId = mpCheckEligibilityRequest.PaymentChannelMetadata.AzureSubscriptionId;

            try
            {
                _logger.Information($"Starting SaaS Purchase Payment Validation for Azure Subscription: {azSubscriptionId}, plan: {mpCheckEligibilityRequest.PlanId}, offer: {mpCheckEligibilityRequest.OfferId}, publisher: {mpCheckEligibilityRequest.PublisherId}");
                var subscriptionLevelPaymentValidationPath = HttpRequestHelper.GetCompleteRequestPathForSubscriptionLevel(azSubscriptionId, resourceGroup, resourceName) + "/eligibilityCheck";
                var additionalHeaders = HttpRequestHelper.GetAdditionalMarketplaceHeaders(mpRequestMetadata);
                var paymentValidationJson = mpCheckEligibilityRequest.ToJObject();
                var validationResponse = await _marketplaceRestClient.SendRequestAsync<MPCheckEligibilityResponse>(HttpMethod.Put, subscriptionLevelPaymentValidationPath, additionalHeaders, paymentValidationJson);

                if (validationResponse == null)
                {
                    _logger.Information($"SaaS Purchase Payment validation failed for Azure Subscription: {azSubscriptionId}, plan: {mpCheckEligibilityRequest.PlanId}, offer: {mpCheckEligibilityRequest.OfferId}, publisher: {mpCheckEligibilityRequest.PublisherId}");
                    return PaymentValidationResponse.BuildValidationResponseFailed(HttpStatusCode.BadRequest, $"SaaS Purchase Payment Check Failed as {nameof(validationResponse)} was null");
                }
                else if (!validationResponse.IsEligible)
                {
                    _logger.Information($"SaaS Purchase Payment validation failed for Azure Subscription: {azSubscriptionId}, plan: {mpCheckEligibilityRequest.PlanId}, offer: {mpCheckEligibilityRequest.OfferId}, publisher: {mpCheckEligibilityRequest.PublisherId}");
                    return PaymentValidationResponse.BuildValidationResponseFailed(HttpStatusCode.BadRequest, $"SaaS Purchase Payment Check Failed as {nameof(validationResponse)} was {validationResponse.ToJsonString()}");
                }
                else
                {
                    _logger.Information($"SaaS Purchase Payment is succesfully validated with response '{validationResponse.ToJsonString()}' for Azure Subscription: {azSubscriptionId}, plan: {mpCheckEligibilityRequest.PlanId}, offer: {mpCheckEligibilityRequest.OfferId}, publisher: {mpCheckEligibilityRequest.PublisherId}");
                }

                return PaymentValidationResponse.BuildValidationResponseSuccessful();
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"Failed to validate SaaS Subscription level payment for the Azure subscription: {azSubscriptionId}. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);

                var requestFailedException = ex as RequestFailedException;

                if (requestFailedException != null)
                {
                    var exceptionMessage = await requestFailedException.Response.Content.ReadAsStringAsync();
                    var statusCode = requestFailedException.Response.StatusCode;
                    _logger.Error(requestFailedException, exceptionMessage);
                    return PaymentValidationResponse.BuildValidationResponseFailed(statusCode, exceptionMessage);
                }

                return PaymentValidationResponse.BuildValidationResponseFailed(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"Failed to validate SaaS Subscription level payment for the Azure subscription: {azSubscriptionId}. Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                return PaymentValidationResponse.BuildValidationResponseFailed(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        public async Task<MarketplaceSubscriptionDetails> GetSubLevelSaasResourceAsync(string azSubscriptionId, string resourceGroup, string resourceName, MarketplaceRequestMetadata requestMetadata)
        {
            if (string.IsNullOrWhiteSpace(azSubscriptionId))
            {
                throw new ArgumentNullException(nameof(azSubscriptionId));
            }

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentNullException(nameof(resourceName));
            }

            if (string.IsNullOrWhiteSpace(resourceGroup))
            {
                throw new ArgumentNullException(nameof(resourceGroup));
            }

            if (requestMetadata is null)
            {
                throw new ArgumentNullException(nameof(requestMetadata));
            }

            using var op = _logger.StartTimedOperation(nameof(GetSubLevelSaasResourceAsync));

            var additionalHeaders = HttpRequestHelper.GetAdditionalMarketplaceHeaders(requestMetadata);
            var requestPath = HttpRequestHelper.GetCompleteRequestPathForSubscriptionLevel(azSubscriptionId, resourceGroup, resourceName);

            try
            {
                var getResponse = await _marketplaceRestClient.SendRequestAsync<MarketplaceSubscriptionDetails>(HttpMethod.Get, requestPath, additionalHeaders);
                _logger.Information($"Get Subscription Level Saas Resource succesful for request {requestPath}");
                return getResponse;
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"Failed to get Subscription Level Saas Resource for request {requestPath} Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw;
            }
        }

        public async Task<MarketplaceSubscriptionDetails> GetTenantLevelSaasResourceAsync(string saasSubscriptionId, MarketplaceRequestMetadata requestMetadata)
        {
            if (string.IsNullOrWhiteSpace(saasSubscriptionId))
            {
                throw new ArgumentNullException(nameof(saasSubscriptionId));
            }

            if (requestMetadata is null)
            {
                throw new ArgumentNullException(nameof(requestMetadata));
            }

            using var op = _logger.StartTimedOperation(nameof(GetTenantLevelSaasResourceAsync));

            var additionalHeaders = HttpRequestHelper.GetAdditionalMarketplaceHeaders(requestMetadata);
            var requestPath = $"{ResourceTypePath}/{saasSubscriptionId}";

            try
            {
                var getResponse = await _marketplaceRestClient.SendRequestAsync<MarketplaceSubscriptionDetails>(HttpMethod.Get, requestPath, additionalHeaders);
                _logger.Information($"Get Tenant Level Saas Resource successful for request {requestPath}");
                return getResponse;
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"Failed to get Tenant Level Saas Resource for request {requestPath} with Error: {ex.Message}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);
                throw;
            }
        }

        public async Task<MigrationResponse> MigrateSaasResourceAsync(string azSubscriptionId, string resourceGroup, string resourceName, MigrationRequest migrationRequest, MarketplaceRequestMetadata requestMetadata)
        {
            if (string.IsNullOrWhiteSpace(azSubscriptionId))
            {
                throw new ArgumentNullException(nameof(azSubscriptionId));
            }

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentNullException(nameof(resourceName));
            }

            if (string.IsNullOrWhiteSpace(resourceGroup))
            {
                throw new ArgumentNullException(nameof(resourceGroup));
            }

            if (migrationRequest is null)
            {
                throw new ArgumentNullException(nameof(migrationRequest));
            }

            if (requestMetadata is null)
            {
                throw new ArgumentNullException(nameof(requestMetadata));
            }

            using var op = _logger.StartTimedOperation(nameof(MigrateSaasResourceAsync));

            var additionalHeaders = HttpRequestHelper.GetAdditionalMarketplaceHeaders(requestMetadata);
            var json = migrationRequest.ToJObject();
            var requestPath = HttpRequestHelper.GetCompleteRequestPathForSubscriptionLevel(azSubscriptionId, resourceGroup, resourceName) + MigrateSaasPath;

            try
            {
                var migrationResponse = await _marketplaceRestClient.SendRequestAsync<HttpResponseMessage>(new HttpMethod("PATCH"), requestPath, additionalHeaders, json);
                _logger.Information($"Saas Migration successful with response {migrationResponse} for request {requestPath}");
                return MigrationResponse.BuildMigrationResponseSuccess();
            }
            catch (MarketplaceException ex)
            {
                string errorMessage = $"Saas Migration failed with Exception {ex.Message} for request {requestPath}";
                _logger.Error(ex, errorMessage);
                op.FailOperation(errorMessage);

                var requestFailedException = ex as RequestFailedException;

                if (requestFailedException != null)
                {
                    var exceptionMessage = await requestFailedException.Response.Content.ReadAsStringAsync();
                    var statusCode = requestFailedException.Response.StatusCode;
                    _logger.Error(requestFailedException, exceptionMessage);
                    return MigrationResponse.BuildMigrationResponseFailed(statusCode, exceptionMessage);
                }

                return MigrationResponse.BuildMigrationResponseFailed(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        private static void ValidateSaaSPurchaseRequest(string resourceName, string resourceGroup, MPCheckEligibilityRequest mpCheckEligibilityRequest, MarketplaceRequestMetadata mpRequestMetadata)
        {
            if (mpCheckEligibilityRequest is null)
            {
                throw new ArgumentNullException(nameof(mpCheckEligibilityRequest), $"Please provide valid {nameof(MPCheckEligibilityRequest)}");
            }
            else if (!mpCheckEligibilityRequest.IsValid())
            {
                throw new MissingFieldException(nameof(mpCheckEligibilityRequest), $"Atleast one of the mandotory fields of '{nameof(mpCheckEligibilityRequest)}' failed IsNullOrWhiteSpace check");
            }

            if (mpRequestMetadata is null)
            {
                throw new ArgumentNullException(nameof(mpRequestMetadata), $"Please provide valid {nameof(MarketplaceRequestMetadata)}");
            }
            else if (!mpRequestMetadata.IsValid())
            {
                throw new MissingFieldException(nameof(mpCheckEligibilityRequest), $"Atleast one of the mandotory fields of '{nameof(mpRequestMetadata)}' failed IsNullOrWhiteSpace check");
            }

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentNullException(nameof(resourceName), $"Please provide valid {nameof(resourceName)}");
            }

            if (string.IsNullOrWhiteSpace(resourceGroup))
            {
                throw new ArgumentNullException(nameof(resourceGroup), $"Please provide valid {nameof(resourceGroup)}");
            }
        }
    }
}
