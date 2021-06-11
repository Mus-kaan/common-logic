//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Flurl;
using Microsoft.Liftr.Marketplace.ARM.Contracts;
using Microsoft.Liftr.Marketplace.ARM.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Liftr.Marketplace.Utils
{
#nullable enable
    internal static class HttpRequestHelper
    {
        private const string MarketplaceLiftrStoreFront = "StoreForLiftr";

        public static HttpRequestMessage CreateRequest(
           string endpoint,
           string apiVersion,
           HttpMethod method,
           string requestPath,
           Guid requestId,
           string correlationId,
           Dictionary<string, string>? additionalHeaders,
           string accessToken = "")
        {
            var requestUrl = endpoint
                .AppendPathSegment(requestPath)
                .SetQueryParam(MarketplaceConstants.DefaultApiVersionParameterName, apiVersion);

            var request = new HttpRequestMessage(method, requestUrl);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            request.Headers.Add(MarketplaceConstants.MarketplaceRequestIdHeaderKey, requestId.ToString());
            request.Headers.Add(MarketplaceConstants.MarketplaceCorrelationIdHeaderKey, correlationId);

            if (additionalHeaders != null)
            {
                foreach (KeyValuePair<string, string> entry in additionalHeaders)
                {
                    request.Headers.Add(entry.Key, entry.Value);
                }
            }

            return request;
        }

        public static Dictionary<string, string> GetAdditionalMarketplaceHeaders(MarketplaceRequestMetadata requestHeaders)
        {
            var additionalHeaders = new Dictionary<string, string>();
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

            if (!string.IsNullOrWhiteSpace(requestHeaders.MSClientSubscriptionId))
            {
                additionalHeaders.Add("x-ms-client-subscription-id", requestHeaders.MSClientSubscriptionId);
            }

            return additionalHeaders;
        }

        public static string GetCompleteRequestPathForAgreement(MarketplaceSaasResourceProperties saasResourceProperties)
        {
            var requestPath = $"subscriptions/{saasResourceProperties.PaymentChannelMetadata.AzureSubscriptionId}/providers/Microsoft.MarketplaceOrdering/offerTypes/SaaS/publishers/{saasResourceProperties.PublisherId}/offers/{saasResourceProperties.OfferId}/plans/{saasResourceProperties.PlanId}/agreements/current";
            return requestPath;
        }

        public static HttpClientHandler GetHttpHandlerForCertAuthentication(X509Certificate2 certificate)
        {
            HttpClientHandler httpRequestHandler = new HttpClientHandler();
            httpRequestHandler.UseDefaultCredentials = true;
            httpRequestHandler.ClientCertificates.Add(certificate);

            return httpRequestHandler;
        }

        public static string GetCompleteRequestPathForSubscriptionLevel(string subscriptionId, string resourceGroup, string resourceName)
        {
            var requestPath = "api/resources/subscriptions/" + subscriptionId + "/resourceGroups/" + resourceGroup + "/" + resourceName;
            return requestPath;
        }
    }
#nullable disable
}
