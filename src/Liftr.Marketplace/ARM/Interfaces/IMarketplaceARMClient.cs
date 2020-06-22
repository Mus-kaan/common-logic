//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Models;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.ARM.Interfaces
{
#nullable enable
    public interface IMarketplaceARMClient
    {
        /// <summary>
        /// Creates a Marketplace Saas resource
        /// </summary>
        /// <returns>Resource Id of the created saas resource</returns>
        Task<string> CreateSaaSResourceAsync(MarketplaceSaasResourceProperties saasResourceProperties, MarketplaceRequestMetadata requestMetadata);

        /// <summary>
        /// Fetch access token for the given market place resource id.
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns>The publisherUri and the access token for the given marketplace resource</returns>
        Task<MarketplaceSaasTokenResponse> GetAccessTokenAsync(string resourceId);
    }
#nullable disable
}
