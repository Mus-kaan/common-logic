//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Serilog;
using System;
using System.Net.Http;

namespace Microsoft.Liftr.Polly
{
    public static class HttpRetryPolicy
    {
        public static Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> GetRetryPolicy()
        {
            // https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry#new-jitter-recommendation
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

            return
                (services, request) =>
                    HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                            delay,
                            onRetry: (outcome, timespan, retryAttempt, context) =>
                            {
                                var logger = services.GetService<ILogger>();
                                logger.Warning("Request: {requestMethod} {requestUrl} failed. Delaying for {delay}ms, then retrying {retry}.", request.Method, request.RequestUri, timespan.TotalMilliseconds, retryAttempt);
                            });
        }
    }
}
