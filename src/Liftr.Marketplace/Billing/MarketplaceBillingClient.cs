//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Flurl;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Marketplace.Billing.Contracts;
using Microsoft.Liftr.Marketplace.Billing.Models;
using Microsoft.Liftr.Marketplace.Billing.Utils;
using Microsoft.Liftr.Marketplace.Options;
using Microsoft.Liftr.Marketplace.Utils;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Billing
{
    public class MarketplaceBillingClient : IMarketplaceBillingClient
    {
        private readonly string _billingBaseUrl;
        private readonly MarketplaceAPIOptions _marketplaceOptions;
        private readonly AuthenticationTokenCallback _authenticationTokenCallback;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public MarketplaceBillingClient(MarketplaceAPIOptions marketplaceOptions, AuthenticationTokenCallback authenticationTokenCallback, ILogger logger, IHttpClientFactory httpClientFactory)
        {
            _marketplaceOptions = marketplaceOptions ?? throw new ArgumentNullException(nameof(marketplaceOptions));
            _authenticationTokenCallback = authenticationTokenCallback ?? throw new ArgumentNullException(nameof(authenticationTokenCallback));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _billingBaseUrl = marketplaceOptions.Endpoint.ToString();
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public MarketplaceBillingClient(MarketplaceAPIOptions marketplaceOptions, AuthenticationTokenCallback authenticationTokenCallback, IHttpClientFactory httpClientFactory)
           : this(marketplaceOptions, authenticationTokenCallback, LoggerFactory.ConsoleLogger, httpClientFactory)
        {
        }

        public delegate Task<string> AuthenticationTokenCallback();

        /// <summary>
        /// Send Usage event to Marketplace for metered billing
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#usage-event
        /// </summary>
        /// <param name="marketplaceUsageEventRequest">Request payload for usage event</param>
        /// <param name="requestMetadata">Http request headers</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Usage event response</returns>
        public async Task<MeteredBillingRequestResponse> SendUsageEventAsync(UsageEventRequest marketplaceUsageEventRequest, BillingRequestMetadata requestMetadata = null, CancellationToken cancellationToken = default)
        {
            var httpClient = _httpClientFactory.CreateClient();

            requestMetadata = SetBillingRequestMetadata(requestMetadata);

            var accessToken = await _authenticationTokenCallback();
            var requestPath = MarketplaceUrlHelper.GetRequestPath(MarketplaceEnum.BillingUsageEvent);
            using var request = CreateRequestWithHeaders(HttpMethod.Post, requestPath, (header) =>
            {
                header.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                header.Add(MarketplaceConstants.MarketplaceRequestIdHeaderKey, requestMetadata.MSRequestId);
                header.Add(MarketplaceConstants.MarketplaceCorrelationIdHeaderKey, requestMetadata.MSCorrelationId);
            });

            var stringContent = JsonConvert.SerializeObject(marketplaceUsageEventRequest);
            request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");

             // the following log message is being used in Common Telemetry System. Please inform the team at liftrcts@microsoft.com if you change the message or format
            _logger.Information($"[{MarketplaceConstants.SAASLogTag} {MarketplaceConstants.BillingLogTag}] [{nameof(SendUsageEventAsync)}] Sending request for UsageEvent: requestUri: {@request.RequestUri}, requestId: {requestMetadata.MSRequestId}, correlationId: {requestMetadata.MSCorrelationId}");

            HttpResponseMessage httpResponse = await httpClient.SendAsync(request, cancellationToken);

            // Temporarily logging Entire Response Headers for Verification
            foreach (var header in httpResponse.Headers)
            {
                _logger.Information($"Header Key: {header.Key}  Header Value: {header.Value.FirstOrDefault()}");
            }

            // Logging Response Headers RequestId and CorrelationId
            var responseCorrelationId = AzureMarketplaceRequestResult.GetIdHeaderValue(httpResponse.Headers, MarketplaceConstants.MarketplaceCorrelationIdHeaderKey);
            var responseRequestId = AzureMarketplaceRequestResult.GetIdHeaderValue(httpResponse.Headers, MarketplaceConstants.MarketplaceRequestIdHeaderKey);

            _logger.Information($"[{MarketplaceConstants.SAASLogTag} {MarketplaceConstants.BillingLogTag}] [{nameof(SendUsageEventAsync)}] Header Response for UsageEvent: requestUri: {@request.RequestUri}, requestId: {responseRequestId}, correlationId: {responseCorrelationId}");

            return httpResponse.StatusCode switch
            {
                HttpStatusCode.OK => await AzureMarketplaceRequestResult.ParseAsync<MeteredBillingSuccessResponse>(httpResponse),
                _ => await BillingUtility.GetMeteredBillingNonSuccessResponseAsync(httpResponse),
            };
        }

        /// <summary>
        /// Send Batch Usage event to Marketplace for metered billing
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#batch-usage-event
        /// </summary>
        /// <param name="marketplaceBatchUsageEventRequest">Request payload for batch usage event</param>
        /// <param name="requestMetadata">Http request headers</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Batch Usage event response</returns>
        public async Task<MeteredBillingRequestResponse> SendBatchUsageEventAsync(BatchUsageEventRequest marketplaceBatchUsageEventRequest, BillingRequestMetadata requestMetadata = null, CancellationToken cancellationToken = default)
        {
            var httpClient = _httpClientFactory.CreateClient();
            requestMetadata = SetBillingRequestMetadata(requestMetadata);

            var accessToken = await _authenticationTokenCallback();
            var requestPath = MarketplaceUrlHelper.GetRequestPath(MarketplaceEnum.BillingBatchUsageEvent);

            using var request = CreateRequestWithHeaders(HttpMethod.Post, requestPath, (header) =>
            {
                header.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                header.Add(MarketplaceConstants.MarketplaceRequestIdHeaderKey, requestMetadata.MSRequestId);
                header.Add(MarketplaceConstants.MarketplaceCorrelationIdHeaderKey, requestMetadata.MSCorrelationId);
            });

            var stringContent = JsonConvert.SerializeObject(marketplaceBatchUsageEventRequest);
            request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");

            _logger.Information($"[{MarketplaceConstants.SAASLogTag} {MarketplaceConstants.BillingLogTag}] [{nameof(SendBatchUsageEventAsync)}] Sending request for BatchUsageEvent: requestUri: {@request.RequestUri}, requestId: {requestMetadata.MSRequestId}, correlationId: {requestMetadata.MSCorrelationId}");

            HttpResponseMessage httpResponse = await httpClient.SendAsync(request, cancellationToken);

            // Temporarily logging Entire Response Headers for Verification
            foreach (var header in httpResponse.Headers)
            {
                _logger.Information($"Header Key: {header.Key}  Header Value: {header.Value.FirstOrDefault()}");
            }

            // Logging Response Headers RequestId and CorrelationId
            var responseCorrelationId = AzureMarketplaceRequestResult.GetIdHeaderValue(httpResponse.Headers, MarketplaceConstants.MarketplaceCorrelationIdHeaderKey);
            var responseRequestId = AzureMarketplaceRequestResult.GetIdHeaderValue(httpResponse.Headers, MarketplaceConstants.MarketplaceRequestIdHeaderKey);

            _logger.Information($"[{MarketplaceConstants.SAASLogTag} {MarketplaceConstants.BillingLogTag}] [{nameof(SendBatchUsageEventAsync)}] Header Response for BatchUsageEvent: requestUri: {@request.RequestUri}, requestId: {responseRequestId}, correlationId: {responseCorrelationId}");

            return httpResponse.StatusCode switch
            {
                HttpStatusCode.OK => await AzureMarketplaceRequestResult.ParseAsync<MeteredBillingBatchUsageSuccessResponse>(httpResponse),
                _ => await BillingUtility.GetMeteredBillingNonSuccessResponseAsync(httpResponse),
            };
        }

        private HttpRequestMessage CreateRequestWithHeaders(HttpMethod method, string requestPath, Action<HttpRequestHeaders> headers = null)
        {
            var endpoint = _billingBaseUrl
                .AppendPathSegment(requestPath)
                .SetQueryParam(MarketplaceConstants.DefaultApiVersionParameterName, _marketplaceOptions.ApiVersion);

            var request = new HttpRequestMessage(method, endpoint);

            headers?.Invoke(request.Headers);

            return request;
        }

        private static BillingRequestMetadata SetBillingRequestMetadata(BillingRequestMetadata requestMetadata)
        {
            if (requestMetadata == null)
            {
                requestMetadata = new BillingRequestMetadata();
            }

            if (string.IsNullOrWhiteSpace(requestMetadata.MSRequestId))
            {
                requestMetadata.MSRequestId = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrWhiteSpace(requestMetadata.MSCorrelationId))
            {
                requestMetadata.MSCorrelationId = TelemetryContext.GetOrGenerateCorrelationId();
            }

            return requestMetadata;
        }
    }
}
