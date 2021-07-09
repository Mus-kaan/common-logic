//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Models;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.ARM.Interfaces
{
    public interface IMarketplaceARMClient
    {
        /// <summary>
        /// Creates a Marketplace Saas resource
        /// </summary>
        /// <returns>Created Saas resource</returns>
        /// <remarks>
        /// https://marketplaceapi.spza-internal.net/swagger/ui/index#!/SubscriptionResourceV2/SubscriptionResourceV2_Put
        /// </remarks>
        Task<MarketplaceSubscriptionDetails> CreateSaaSResourceAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Creates a Marketplace SaaS resource at Subscription Level
        /// </summary>
        /// <returns>Created SaaS resource</returns>
        Task<MarketplaceSubscriptionDetails> CreateSaaSResourceAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata, string resourceGroup);

        /// <summary>
        /// Deletes a Marketplace Saas resource
        /// </summary>
        /// <remarks>
        /// https://marketplaceapi.spza-internal.net/swagger/ui/index#!/SubscriptionResourceV2/SubscriptionResourceV2_Delete
        /// </remarks>
        Task DeleteSaaSResourceAsync(MarketplaceSubscription marketplaceSubscription, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Deletes a Marketplace Saas resource at subscription level
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceName"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="requestMetadata"></param>
        Task DeleteSaaSResourceAsync(string subscriptionId, string resourceName, string resourceGroup, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Fetch access token for the given market place resource id.
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns>The publisherUri and the access token for the given marketplace resource</returns>
        /// <remarks>
        /// https://marketplaceapi.spza-internal.net/swagger/ui/index#!/SubscriptionResourceV2/SubscriptionResourceV2_Post_0
        /// </remarks>
        Task<MarketplaceSaasTokenResponse> GetAccessTokenAsync(string resourceId);

        /// <summary>
        /// Validates the SaaS Purchase Payment
        /// </summary>
        /// <param name="paymentValidationRequest"></param>
        /// <param name="requestMetadata"></param>
        /// <returns>Payment validation status</returns>
        Task<PaymentValidationResponse> ValidatesSaaSPurchasePaymentAsync(PaymentValidationRequest paymentValidationRequest, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Migrates tenant level Saas resource to Subscription level
        /// </summary>
        /// <param name="azSubscriptionId"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="resourceName"></param>
        /// <param name="migrationRequest"></param>
        /// <param name="requestMetadata"></param>
        /// <returns></returns>
        Task<MigrationResponse> MigrateSaasResourceAsync(string azSubscriptionId, string resourceGroup, string resourceName, MigrationRequest migrationRequest, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Gets Saas resource details using subscription level API
        /// </summary>
        /// <param name="azSubscriptionId"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="resourceName"></param>
        /// <param name="requestMetadata"></param>
        /// <returns>Saas resource details</returns>
        Task<MarketplaceSubscriptionDetails> GetSubLevelSaasResourceAsync(string azSubscriptionId, string resourceGroup, string resourceName, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Gets Saas resource details using tenant level API
        /// </summary>
        /// <param name="saasSubscriptionId"></param>
        /// <param name="requestMetadata"></param>
        /// <returns>Saas resource details</returns>
        Task<MarketplaceSubscriptionDetails> GetTenantLevelSaasResourceAsync(string saasSubscriptionId, MarketplaceRequestMetadata requestMetadata);
    }
}
