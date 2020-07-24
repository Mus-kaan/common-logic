//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Flurl;
using Flurl.Http;
using Microsoft.Liftr.Marketplace.Billing.Contracts;
using Microsoft.Liftr.Marketplace.Billing.Models;
using Microsoft.Liftr.Marketplace.Billing.Utils;
using Microsoft.Liftr.Marketplace.Saas.Options;
using Newtonsoft.Json;
using Serilog;
using System;
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
        private const string DefaultApiVersionParameterName = "api-version";
        private readonly string _billingBaseUrl;
        private readonly MarketplaceSaasOptions _marketplaceOptions;
        private readonly AuthenticationTokenCallback _authenticationTokenCallback;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public MarketplaceBillingClient(MarketplaceSaasOptions marketplaceOptions, AuthenticationTokenCallback authenticationTokenCallback, ILogger logger, HttpClient client)
        {
            _marketplaceOptions = marketplaceOptions ?? throw new ArgumentNullException(nameof(marketplaceOptions));
            _authenticationTokenCallback = authenticationTokenCallback ?? throw new ArgumentNullException(nameof(authenticationTokenCallback));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _billingBaseUrl = marketplaceOptions.API.Endpoint.ToString();
            _httpClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        public delegate Task<string> AuthenticationTokenCallback();

        /// <summary>
        /// Send Usage event to Marketplace for metered billing
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis#usage-event
        /// </summary>
        /// <param name="marketplaceUsageEventRequest">Request payload for usage event</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Usage event response</returns>
        public async Task<MeteredBillingRequestResponse> SendUsageEventAsync(UsageEventRequest marketplaceUsageEventRequest, CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid(); // Every request should have a different requestId
            var accessToken = await _authenticationTokenCallback();
            var requestPath = Constants.UsageEventPath;
            using var request = CreateRequestWithHeaders(HttpMethod.Post, requestPath, requestId, accessToken);
            var stringContent = JsonConvert.SerializeObject(marketplaceUsageEventRequest);
            request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");

            _logger.Information("Sending request for usageevent: requestUri: {@requestUrl}, requestId: {requestId}", request.RequestUri, requestId);

            HttpResponseMessage httpResponse = await _httpClient.SendAsync(request, cancellationToken);

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
        /// <param name="cancellationToken"></param>
        /// <returns>Batch Usage event response</returns>
        public async Task<MeteredBillingRequestResponse> SendBatchUsageEventAsync(BatchUsageEventRequest marketplaceBatchUsageEventRequest, CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid(); // Every request should have a different requestId
            var accessToken = await _authenticationTokenCallback();
            var requestPath = Constants.BatchUsageEventPath;
            using var request = CreateRequestWithHeaders(HttpMethod.Post, requestPath, requestId, accessToken);
            var stringContent = JsonConvert.SerializeObject(marketplaceBatchUsageEventRequest);
            request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");

            _logger.Information("Sending request for usageevent: requestUri: {@requestUrl}, requestId: {requestId}", request.RequestUri, requestId);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.OK => await AzureMarketplaceRequestResult.ParseAsync<MeteredBillingBatchUsageSuccessResponse>(response),
                _ => await BillingUtility.GetMeteredBillingNonSuccessResponseAsync(response),
            };
        }

        private HttpRequestMessage CreateRequestWithHeaders(HttpMethod method, string requestPath, Guid requestId, string accessToken)
        {
            var endpoint = _billingBaseUrl
                .AppendPathSegment(requestPath)
                .SetQueryParam(DefaultApiVersionParameterName, _marketplaceOptions.API.ApiVersion);

            var request = new HttpRequestMessage(method, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("x-ms-requestid", requestId.ToString());

            return request;
        }
    }
}
