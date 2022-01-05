//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Liftr.IdempotentRPWorker.Constants;
using Microsoft.Liftr.IdempotentRPWorker.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Models;

namespace Microsoft.Liftr.IdempotentRPWorker.Utils
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Get RP request headers
        /// </summary>
        /// <param name="request">http request</param>
        /// <returns></returns>
        public static PartnerRequestMetaData GetRequestMetaDataHeaders(this HttpRequest request)
        {
            var requestHeaders = request?.Headers;

            if (requestHeaders == null)
            {
                // No need to add the headers as it has no request headers.
                return null;
            }

            var mpData = new MarketplaceRequestMetadata
            {
                MSClientObjectId = GetHeaderValue(requestHeaders, RPHeaderConstants.ClientObjectIdKey),
                MSClientTenantId = GetHeaderValue(requestHeaders, RPHeaderConstants.ClientTenantIdKey),
                MSClientIssuer = GetHeaderValue(requestHeaders, RPHeaderConstants.ClientIssuerKey),
                MSClientPrincipalId = GetHeaderValue(requestHeaders, RPHeaderConstants.ClientPrincipalIdKey),
                MSClientPrincipalName = GetHeaderValue(requestHeaders, RPHeaderConstants.ClientPrincipalNameKey),
                MSClientGroupMembership = GetHeaderValue(requestHeaders, RPHeaderConstants.ClientGroupMembershipKey),
            };

            var miData = new ManagedIdentityRequestMetadata()
            {
                IdentityUrl = GetHeaderValue(requestHeaders, RPHeaderConstants.IdentityUrlKey),
                IdentityPrincipalId = GetHeaderValue(requestHeaders, RPHeaderConstants.IdentityPrincipalIdKey),
                HomeTenantId = GetHeaderValue(requestHeaders, RPHeaderConstants.HomeTenantIdKey),
            };

            return new PartnerRequestMetaData
            {
                MarketplaceMetadata = mpData,
                ManagedIdentityMetadata = miData,
            };
        }

        /// <summary>
        /// Get request header
        /// </summary>
        /// <param name="headers">http header</param>
        /// <param name="keyName">header name</param>
        /// <returns></returns>
        public static string GetHeaderValue(IHeaderDictionary headers, string keyName)
        {
            if (!string.IsNullOrWhiteSpace(keyName) && headers != null)
            {
                if (headers.ContainsKey(keyName))
                {
                    return headers[keyName];
                }
            }

            return string.Empty;
        }
    }
}
