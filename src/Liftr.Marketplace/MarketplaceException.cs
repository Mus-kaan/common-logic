//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Exceptions
{
    public class MarketplaceException : Exception
    {
        public MarketplaceException(string message)
            : base(message)
        {
        }

        public MarketplaceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MarketplaceException()
        {
        }

        public HttpResponseMessage Response { get; set; }

        public Uri RequestUri { get; set; }

        public static async Task<MarketplaceException> CreateMarketplaceExceptionAsync(HttpResponseMessage response, string errorMessage)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (response.Content != null)
            {
                errorMessage += $" Content: {await response.Content.ReadAsStringAsync()}";
            }

            var marketplaceException = new MarketplaceException(errorMessage)
            {
                RequestUri = response.RequestMessage.RequestUri,
                Response = response,
            };

            return marketplaceException;
        }
    }
}
