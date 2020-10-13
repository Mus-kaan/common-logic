//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Flurl;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Marketplace.Contracts;
using Microsoft.Liftr.Marketplace.Exceptions;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Marketplace.Tests")]

namespace Microsoft.Liftr.Marketplace
{
    public class MarketplaceRestClient
    {
        private const string AsyncOperationLocation = "Operation-Location";
        private const string DefaultApiVersionParameterName = "api-version";
        private readonly string _endpoint;
        private readonly string _apiVersion;
        private readonly ILogger _logger;
        private readonly AuthenticationTokenCallback _authenticationTokenCallback;
        private readonly IHttpClientFactory _httpClientFactory;

        public MarketplaceRestClient(
            Uri endpoint,
            string apiVersion,
            ILogger logger,
            IHttpClientFactory httpClientFactory,
            AuthenticationTokenCallback authenticationTokenCallback)
        {
            if (endpoint is null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (string.IsNullOrEmpty(apiVersion))
            {
                throw new ArgumentException("message", nameof(apiVersion));
            }

            _endpoint = endpoint.ToString();
            _apiVersion = apiVersion;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _authenticationTokenCallback = authenticationTokenCallback ?? throw new ArgumentNullException(nameof(authenticationTokenCallback));
        }

        public delegate Task<string> AuthenticationTokenCallback();

#nullable enable

        /// <summary>
        /// Send requests for the Marketplace Saas fulfillment APIs and return the response
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> SendRequestAsync<T>(
            HttpMethod method,
            string requestPath,
            Dictionary<string, string>? additionalHeaders = null,
            object? content = null,
            CancellationToken cancellationToken = default) where T : class
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var requestId = Guid.NewGuid(); // Every request should have a different requestId
            var correlationId = TelemetryContext.GetOrGenerateCorrelationId();

            var accessToken = await _authenticationTokenCallback();

            using var request = CreateRequestWithHeaders(method, requestPath, requestId, correlationId, additionalHeaders, accessToken);
            _logger.Information($"Sending request method: {method}, requestUri: {request.RequestUri}, requestId: {requestId}, correlationId: {correlationId} for SAAS fulfillment or create");
            HttpResponseMessage? httpResponse = null;

            try
            {
                if (content != null)
                {
                    var requestBody = JsonConvert.SerializeObject(content);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    httpResponse = await httpClient.SendAsync(request, cancellationToken);
                }
                else
                {
                    httpResponse = await httpClient.SendAsync(request, cancellationToken);
                }

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw await GetRequestFailedExceptionAsync(httpResponse);
                }

                var response = (await httpResponse.Content.ReadAsStringAsync()).FromJson<T>();
                _logger.Information($"Request: {request.RequestUri} succeded for SAAS fulfillment or create");

                return response;
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"The request: {method}:{request.RequestUri} failed for SAAS fulfillment or create";
                if (ex.Message != null)
                {
                    errorMessage += $"Reason: {ex?.Message}";
                }

                var marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(httpResponse, errorMessage);
                _logger.Error(marketplaceException, errorMessage);
                throw marketplaceException;
            }
        }

        public async Task<T> SendRequestWithPollingAsync<T>(
            HttpMethod method,
            string requestPath,
            Dictionary<string, string>? additionalHeaders = null,
            object? content = null,
            CancellationToken cancellationToken = default) where T : MarketplaceAsyncOperationResponse
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var requestId = Guid.NewGuid(); // Every request should have a different requestId
            var correlationId = TelemetryContext.GetOrGenerateCorrelationId();

            var accessToken = await _authenticationTokenCallback();

            using var request = CreateRequestWithHeaders(method, requestPath, requestId, correlationId, additionalHeaders, accessToken);
            _logger.Information($"Sending request method: {method}, requestUri: {request.RequestUri}, requestId: {requestId}, correlationId: {correlationId} for SAAS fulfillment or create");
            HttpResponseMessage? httpResponse = null;

            try
            {
                if (content != null)
                {
                    var requestBody = JsonConvert.SerializeObject(content);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    httpResponse = await httpClient.SendAsync(request, cancellationToken);
                }
                else
                {
                    httpResponse = await httpClient.SendAsync(request, cancellationToken);
                }

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw await GetRequestFailedExceptionAsync(httpResponse);
                }

                if (httpResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    // If the status code is 202 means it is an async operation
                    return await PollOperationAsync<T>(request, httpResponse, httpClient, 20);
                }

                var response = (await httpResponse.Content.ReadAsStringAsync()).FromJson<T>();
                _logger.Information($"Request: {request.RequestUri} succeded for SAAS fulfillment or create.");
                return response;
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"The request: {method}:{request.RequestUri} failed for SAAS fulfillment or create.";
                if (ex.Message != null)
                {
                    errorMessage += $"Reason: {ex?.Message}";
                }

                var marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(httpResponse, errorMessage);
                _logger.Error(marketplaceException, errorMessage);
                throw marketplaceException;
            }
        }

        private HttpRequestMessage CreateRequestWithHeaders(
           HttpMethod method,
           string requestPath,
           Guid requestId,
           string correlationId,
           Dictionary<string, string>? additionalHeaders,
           string accessToken)
        {
            var requestUrl = _endpoint
                .AppendPathSegment(requestPath)
                .SetQueryParam(DefaultApiVersionParameterName, _apiVersion);

            var request = new HttpRequestMessage(method, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add(MarketplaceConstants.MarketplaceRequestIdHeaderKey, requestId.ToString());
            request.Headers.Add(MarketplaceConstants.MarketplaceCorrelationIdHeaderKey, correlationId);

            if (additionalHeaders != null)
            {
                foreach (KeyValuePair<string, string> entry in additionalHeaders)
                {
                    request.Headers.Add(entry.Key, entry.Value);
                }
            }

            return request;
        }

        private static Uri GetOperationLocationFromHeader(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues(AsyncOperationLocation, out IEnumerable<string> azoperationLocations))
            {
                return new Uri(azoperationLocations.Single());
            }
            else
            {
                string errorMessage = $"Could not get Operation-Location header from response of async polling for SAAS resource creation. Request Uri : {response?.RequestMessage?.RequestUri}";
                throw new MarketplaceHttpException(errorMessage);
            }
        }

        private TimeSpan GetRetryAfterValue(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter == null)
            {
                var errorMessage = $"Could not parse correct headers from operation response of async polling for SAAS resource creation. Request Uri : {response?.RequestMessage?.RequestUri}";
                var marketplaceException = new MarketplaceHttpException(errorMessage);
                _logger.Error(marketplaceException, errorMessage);
                throw marketplaceException;
            }

            return retryAfter.Value;
        }

        private async Task<T> PollOperationAsync<T>(HttpRequestMessage originalRequest, HttpResponseMessage response, HttpClient httpClient, int retryCounter) where T : class
        {
            // Read all the relevant headers from the original 202 response
            var retryAfter = GetRetryAfterValue(response);
            var resultLocation = GetOperationLocationFromHeader(response);

            if (retryCounter == 0)
            {
                string errorMessage = $"Maximum retries of async polling for SAAS resource creation has been reached. So terminating the polling requests. Operation Id : {resultLocation}";
                var marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(response, errorMessage);
                _logger.Error(marketplaceException, errorMessage);
                throw marketplaceException;
            }

            // Wait as long as was requested
            await Task.Delay(retryAfter);

            using var operationLocationRequest = GetSubrequestMessage(originalRequest, resultLocation);
            var asyncOperationResponse = await httpClient.SendAsync(operationLocationRequest);

            if (!asyncOperationResponse.IsSuccessStatusCode)
            {
                throw await GetRequestFailedExceptionAsync(asyncOperationResponse);
            }

            // you will have to check here the response and if that says complete then you can proceed or "status": "InProgress" then do it again.
            if (asyncOperationResponse.StatusCode == HttpStatusCode.OK)
            {
                var asyncResponseObj = (await asyncOperationResponse.Content.ReadAsStringAsync()).FromJson<MarketplaceAsyncOperationResponse>();

                switch (asyncResponseObj.Status)
                {
                    case OperationStatus.InProgress:
                    case OperationStatus.NotStarted:
                        _logger.Information($"Trying to check the status of operation again. {resultLocation}.Operation status for SAAS resource creation is {asyncResponseObj.Status}");
                        return await PollOperationAsync<T>(originalRequest, response, httpClient, --retryCounter);

                    case OperationStatus.Failed:
                        string errorMessage = $"Async polling operation failed while polling the operation. Operation Id : {resultLocation} for SAAS resource creation";
                        var marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(asyncOperationResponse, errorMessage);
                        _logger.Error(marketplaceException, errorMessage);
                        throw marketplaceException;

                    case OperationStatus.Succeeded:
                        var message = $"Async polling operation has beeen successful for SAAS resource creation. Operation Id : {resultLocation}";
                        _logger.Information(message);
                        return (await asyncOperationResponse.Content.ReadAsStringAsync()).FromJson<T>();

                    default:
                        errorMessage = $"Unknown operation is detected for async polling of marketplace API for SAAS resource creation. Operation Id : {resultLocation} and status : {asyncResponseObj.Status}";
                        marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(asyncOperationResponse, errorMessage);
                        _logger.Error(marketplaceException, errorMessage);
                        throw marketplaceException;
                }
            }
            else
            {
                string errorMessage = $"Cannot get the status of polling operation for SAAS resource creation. Operation Id : {resultLocation} Status Code: {asyncOperationResponse.StatusCode}";
                var marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(asyncOperationResponse, errorMessage);
                _logger.Error(marketplaceException, errorMessage);
                throw marketplaceException;
            }
        }

        private static HttpRequestMessage GetSubrequestMessage(HttpRequestMessage originalRequest, Uri resultLocation)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, resultLocation);

            foreach (var header in originalRequest.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            return request;
        }

        private async Task<MarketplaceHttpException> GetRequestFailedExceptionAsync(HttpResponseMessage httpResponse)
        {
            var responseContent = httpResponse.Content?.ReadAsStringAsync();
            var errorMessage = $"SAAS Fulfillment or Create Request Failed with status code: {httpResponse.StatusCode} and content: {responseContent}";
            var marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(httpResponse, errorMessage);
            _logger.Error(marketplaceException, errorMessage);
            return marketplaceException;
        }
#nullable disable
    }
}
