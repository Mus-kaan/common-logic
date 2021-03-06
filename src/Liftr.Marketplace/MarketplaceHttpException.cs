//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Exceptions
{
    public class MarketplaceHttpException : Exception
    {
        public MarketplaceHttpException(string message)
            : base(message)
        {
        }

        public MarketplaceHttpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MarketplaceHttpException()
        {
        }

        public HttpResponseMessage Response { get; set; }

        public Uri RequestUri { get; set; }

        public static async Task<MarketplaceHttpException> CreateMarketplaceHttpExceptionAsync(HttpResponseMessage response, string errorMessage)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (response.Content != null)
            {
                errorMessage += $" Content: {await response.Content.ReadAsStringAsync()}";
            }

            var marketplaceException = new MarketplaceHttpException(errorMessage)
            {
                RequestUri = response.RequestMessage.RequestUri,
                Response = response,
            };

            return marketplaceException;
        }

        internal static async Task<MarketplaceHttpException> CreateRequestFailedExceptionAsync(HttpResponseMessage httpResponse)
        {
            string responseContent = string.Empty;
            if (httpResponse.Content != null)
            {
                responseContent = await httpResponse.Content.ReadAsStringAsync();
            }

            var errorMessage = $"SAAS Fulfillment or Create Request Failed with status code: {httpResponse.StatusCode} and content: {responseContent}";
            var marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(httpResponse, errorMessage);
            return marketplaceException;
        }
    }
}
