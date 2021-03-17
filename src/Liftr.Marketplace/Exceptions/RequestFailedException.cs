//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Exceptions
{
    public class RequestFailedException : MarketplaceException
    {
        public RequestFailedException(string message)
            : base(message)
        {
        }

        public RequestFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public RequestFailedException()
        {
        }

        public HttpResponseMessage Response { get; set; }

        public Uri RequestUri { get; set; }

        internal static async Task<RequestFailedException> CreateAsync(HttpRequestMessage request, HttpResponseMessage response)
        {
            var message = await ExceptionMessageUtils.BuildRequestFailedMessageAsync(request, response);

            var marketplaceException = new RequestFailedException(message)
            {
                RequestUri = response.RequestMessage.RequestUri,
                Response = response,
            };

            return marketplaceException;
        }
    }
}
