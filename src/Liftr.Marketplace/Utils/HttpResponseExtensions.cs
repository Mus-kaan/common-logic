//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace Microsoft.Liftr.Marketplace.Utils
{
    internal static class HttpResponseExtensions
    {
        public static Uri GetOperationLocationFromHeader(this HttpResponseMessage response, ILogger logger)
        {
            if (response.Headers.TryGetValues(MarketplaceConstants.AsyncOperationLocation, out IEnumerable<string> azoperationLocations))
            {
                return new Uri(azoperationLocations.Single());
            }
            else
            {
                string errorMessage = $"Could not get Operation-Location header from response of async polling for SAAS resource creation. Request Uri : {response?.RequestMessage?.RequestUri}";
                throw new MarketplaceHttpException(errorMessage);
            }
        }

        public static TimeSpan GetRetryAfterValue(this HttpResponseMessage response, ILogger logger)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter == null)
            {
                var errorMessage = $"Could not parse correct headers from operation response of async polling for SAAS resource creation. Request Uri : {response?.RequestMessage?.RequestUri}";
                var marketplaceException = new MarketplaceHttpException(errorMessage);
                logger.Error(marketplaceException, errorMessage);
                throw marketplaceException;
            }

            return retryAfter.Value;
        }
    }
}
