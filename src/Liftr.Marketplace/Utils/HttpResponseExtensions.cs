//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

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
                throw new InvalidOperationException("Missing Operation-Location value from response header");
            }
        }

        public static TimeSpan GetRetryAfterValue(this HttpResponseMessage response, ILogger logger)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter == null)
            {
                throw new InvalidOperationException("Missing RetryAfter value from response header");
            }

            return retryAfter.Value;
        }
    }
}
