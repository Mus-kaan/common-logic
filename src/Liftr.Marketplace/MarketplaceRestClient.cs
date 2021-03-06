//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Marketplace.Contracts;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Utils;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Marketplace.Tests")]

namespace Microsoft.Liftr.Marketplace
{
    public class MarketplaceRestClient
    {
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

        public MarketplaceRestClient(
            Uri endpoint,
            string apiVersion,
            IHttpClientFactory httpClientFactory,
            AuthenticationTokenCallback authenticationTokenCallback)
            : this(endpoint, apiVersion, LoggerFactory.ConsoleLogger, httpClientFactory, authenticationTokenCallback)
        {
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

            using var request = HttpRequestHelper.CreateRequest(_endpoint, _apiVersion, method, requestPath, requestId, correlationId, additionalHeaders, accessToken);
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
                    throw await MarketplaceHttpException.CreateRequestFailedExceptionAsync(httpResponse);
                }

                var response = (await httpResponse.Content.ReadAsStringAsync()).FromJson<T>();
                if (method == HttpMethod.Get)
                {
                    // Temporarily printing the response content to debug an issue with Saas GET request
                    _logger.Information($"Request Content for GET request: {await httpResponse.Content.ReadAsStringAsync()}");
                }

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
            CancellationToken cancellationToken = default) where T : BaseOperationResponse
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var requestId = Guid.NewGuid(); // Every request should have a different requestId
            var correlationId = TelemetryContext.GetOrGenerateCorrelationId();

            var accessToken = await _authenticationTokenCallback();

            using var request = HttpRequestHelper.CreateRequest(_endpoint, _apiVersion, method, requestPath, requestId, correlationId, additionalHeaders, accessToken);
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
                    throw await MarketplaceHttpException.CreateRequestFailedExceptionAsync(httpResponse);
                }

                if (httpResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    // If the status code is 202 means it is an async operation
                    var poller = new AsyncOperationPoller(request, httpResponse, httpClient, _logger);
                    return await poller.PollOperationAsync<T>(MarketplaceConstants.PollingCount);
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
#nullable disable
    }
}
