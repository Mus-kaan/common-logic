//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

namespace Microsoft.Liftr.Fluent
{
    public static class HttpPolicyHelper
    {
        public static IAsyncPolicy<HttpResponseMessage> GetDefaultPolicy()
        {
            // https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry#new-jitter-recommendation
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(3), retryCount: 5);

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(delay);
        }
    }
}
