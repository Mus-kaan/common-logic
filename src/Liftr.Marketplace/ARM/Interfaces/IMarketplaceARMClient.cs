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
    }
}
