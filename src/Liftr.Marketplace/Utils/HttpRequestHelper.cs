//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Flurl;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Liftr.Marketplace.Utils
{
#nullable enable
    internal static class HttpRequestHelper
    {
        public static HttpRequestMessage CreateRequest(
           string endpoint,
           string apiVersion,
           HttpMethod method,
           string requestPath,
           Guid requestId,
           string correlationId,
           Dictionary<string, string>? additionalHeaders,
           string accessToken)
        {
            var requestUrl = endpoint
                .AppendPathSegment(requestPath)
                .SetQueryParam(MarketplaceConstants.DefaultApiVersionParameterName, apiVersion);

            var request = new HttpRequestMessage(method, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add(MarketplaceConstants.MarketplaceRequestIdHeaderKey, requestId.ToString());
            request.Headers.Add(MarketplaceConstants.MarketplaceCorrelationIdHeaderKey, correlationId);
            request.Headers.Add(MarketplaceConstants.MetricTypeHeaderKey, MarketplaceConstants.MetricTypeHeaderValue);

            if (additionalHeaders != null)
            {
                foreach (KeyValuePair<string, string> entry in additionalHeaders)
                {
                    request.Headers.Add(entry.Key, entry.Value);
                }
            }

            return request;
        }
    }
#nullable disable
}
