//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Microsoft.Liftr.RPaaS
{
    internal static class HttpPolicies
    {
        private const double FirstRetryDelay = 3;
        private const int RetryCount = 5;

        /// <summary>
        /// For patch operation we need to retry on 404 as sometimes due to ARM cache replication issue, we get 404 on first attempt
        /// </summary>
        public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicyForNotFound(ILogger logger)
        {
            var delay = GetJitteredBackoffDelay();

            HttpStatusCode[] httpStatusCodesWorthRetrying =
           {
                   HttpStatusCode.NotFound, // 404
           };

            return GetPolicy(httpStatusCodesWorthRetrying, delay, logger);
        }

        private static AsyncRetryPolicy<HttpResponseMessage> GetPolicy(HttpStatusCode[] httpStatusCodesWorthRetrying, IEnumerable<TimeSpan> delay, ILogger logger)
        {
            return Policy
                  .HandleResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                  .WaitAndRetryAsync(
                      delay,
                      onRetry: (outcome, timespan, retryAttempt, context) =>
                      {
                          LogRetryInfo(outcome, timespan, retryAttempt, logger);
                      });
        }

        /// <summary>
        /// Reference: https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry#new-jitter-recommendation
        /// </summary>
        private static IEnumerable<TimeSpan> GetJitteredBackoffDelay() => Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(FirstRetryDelay), retryCount: RetryCount);

        private static void LogRetryInfo(DelegateResult<HttpResponseMessage> outcome, TimeSpan timespan, int retryAttempt, ILogger logger)
        {
            logger.Information("Request: {requestMethod} {requestUrl} failed. Delaying for {delay}ms, then retrying attempt is: {retry} / {totalCount}.", outcome.Result.RequestMessage?.Method, outcome.Result.RequestMessage?.RequestUri, timespan.TotalMilliseconds, retryAttempt, RetryCount);
        }
    }
}
