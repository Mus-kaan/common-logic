//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.Marketplace.ARM.Models
{
    public class MarketplaceRequestMetadata
    {
        [JsonProperty(PropertyName = "x-ms-client-object-id")]
        public string MSClientObjectId { get; set; }

        [JsonProperty(PropertyName = "x-ms-client-tenant-id")]
        public string MSClientTenantId { get; set; }

        [JsonProperty(PropertyName = "x-ms-client-principal-name")]
        public string MSClientPrincipalName { get; set; }

        [JsonProperty(PropertyName = "x-ms-client-principal-id")]
        public string MSClientPrincipalId { get; set; }

        [JsonProperty(PropertyName = "x-ms-client-issuer")]
        public string MSClientIssuer { get; set; }

        [JsonProperty(PropertyName = "x-ms-client-app-id")]
        public string MSClientAppId { get; set; }

        [JsonProperty(PropertyName = "x-ms-client-subscription-id")]
        public string MSClientSubscriptionId { get; set; }

        public bool IsValid()
        {
            // TODO: figure out which ones are the really used ones.
            // 'MSClientPrincipalId'and 'MSClientPrincipalName' are empty for serivce principal.
            // 'MSClientObjectId' is empty for CSP Subscription
            return !string.IsNullOrEmpty(MSClientTenantId);
        }
    }
}
