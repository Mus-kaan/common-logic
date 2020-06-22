//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Options;
using Microsoft.Liftr.TokenManager.Options;

namespace Microsoft.Liftr.Marketplace.Saas.Options
{
    public class MarketplaceSaasOptions
    {
        /// <summary>
        /// The marketplace endpoint for the fulfillment and billing
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#mock-apis
        /// </summary>
        public MarketplaceAPIOptions API { get; set; } = null!;

        /// <summary>
        /// This is the configuration needed to authenticate to the Marketplace APIs
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-registration#using-the-azure-ad-security-token
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/create-new-saas-offer#technical-configuration
        /// </summary>
        /// <remarks>Target resource will be as described in the above url</remarks>
        public SingleTenantAADAppTokenProviderOptions SaasOfferTechnicalConfig { get; set; } = null!;
    }
}
