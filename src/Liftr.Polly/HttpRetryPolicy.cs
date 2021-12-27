//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Serilog;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
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

        public static IAsyncPolicy<HttpResponseMessage> GetMarketplaceRetryPolicyForEntityNotFound(ILogger logger)
        {
            HttpStatusCode[] httpStatusCodesWorthRetrying =
            {
                   HttpStatusCode.BadRequest, // 400
            };

            return GetRetryPolicy(logger, PollyConstants.MarketplaceRetryForEntityNotFoundCount, PollyConstants.MarketplaceRetryForEntityNotFoundLogTag, httpStatusCodesWorthRetrying);
        }

        public static IAsyncPolicy<HttpResponseMessage> GetDefaultMarketplaceRetryPolicy(ILogger logger)
        {
            return GetRetryPolicy(logger, PollyConstants.MarketplaceRetryCount, PollyConstants.MarketplaceRetryLogTag);
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger, int totalRetryCount, string executorLogTag, HttpStatusCode[] httpStatusCodesWorthRetrying = default)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<OperationCanceledException>()
                .OrResult(r => httpStatusCodesWorthRetrying != null && httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(
                    retryCount: totalRetryCount,
                    sleepDurationProvider: (retryCount, response, context) => GetServerWaitDuration(response),
                    onRetryAsync: async (response, timespan, retryCount, context) =>
                    {
                        var httpResponseContent = await response?.Result?.Content?.ReadAsStringAsync();
                        logger.Error($"[{executorLogTag}] [{nameof(GetRetryPolicy)}] Requested operation to retry failed with content {httpResponseContent} and retryAttempt is {retryCount}/{totalRetryCount}");
                    });
        }

        private static TimeSpan GetServerWaitDuration(DelegateResult<HttpResponseMessage> httpResponse)
        {
            var retryAfterValue = httpResponse?.Result?.Headers?.RetryAfter?.Delta;
            var retryValue = (retryAfterValue != null && retryAfterValue.HasValue) ? Convert.ToInt16(retryAfterValue.Value.TotalSeconds, CultureInfo.InvariantCulture) : PollyConstants.CacheRefreshRetryWait;

            return TimeSpan.FromSeconds(retryValue);
        }
    }
}
