//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Flurl;
using Flurl.Http;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Marketplace.Contracts;
using Microsoft.Liftr.Marketplace.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
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

        public MarketplaceRestClient(
            Uri endpoint,
            string apiVersion,
            ILogger logger,
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
            var requestId = Guid.NewGuid(); // Every request should have a different requestId
            var correlationId = TelemetryContext.GetOrGenerateCorrelationId();

            var accessToken = await _authenticationTokenCallback();

            var request = CreateRequestWithHeaders(requestPath, requestId, additionalHeaders, accessToken);
            _logger.Information("Sending request method: {@method}, requestUri: {@requestUrl}, requestId: {requestId}", method, request.Url, requestId);

            try
            {
                HttpResponseMessage httpResponse;

                if (content != null)
                {
                    httpResponse = await request.SendJsonAsync(method, content, cancellationToken: cancellationToken);
                }
                else
                {
                    httpResponse = await request.SendAsync(method, cancellationToken: cancellationToken);
                }

                var response = (await httpResponse.Content.ReadAsStringAsync()).FromJson<T>();
                _logger.Information("Request: {@requestUrl} succeded", request.Url);

                return response;
            }
            catch (FlurlHttpException ex)
            {
                var errorMessage = $"The request: {method}:{request.Url} failed.";
                if (ex.Call.Response != null && !string.IsNullOrEmpty(ex.Call.Response.ReasonPhrase))
                {
                    errorMessage += $"Reason: {ex.Call.Response.ReasonPhrase}";
                }

                var marketplaceException = await MarketplaceException.CreateMarketplaceExceptionAsync(ex.Call.Response, errorMessage);
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
            var requestId = Guid.NewGuid(); // Every request should have a different requestId
            var correlationId = TelemetryContext.GetOrGenerateCorrelationId();

            var accessToken = await _authenticationTokenCallback();

            var request = CreateRequestWithHeaders(requestPath, requestId, additionalHeaders, accessToken);
            _logger.Information("Sending request method: {@method}, requestUri: {@requestUrl}, requestId: {requestId}", method, request.Url, requestId);

            try
            {
                HttpResponseMessage httpResponse;

                if (content != null)
                {
                    httpResponse = await request.SendJsonAsync(method, content, cancellationToken: cancellationToken);
                }
                else
                {
                    httpResponse = await request.SendAsync(method, cancellationToken: cancellationToken);
                }

                if (httpResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    // If the status code is 202 means it is an async operation
                    return await PollOperationAsync<T>(request, httpResponse, 20);
                }

                var response = (await httpResponse.Content.ReadAsStringAsync()).FromJson<T>();
                _logger.Information("Request: {@requestUrl} succeded", request.Url);
                return response;
            }
            catch (FlurlHttpException ex)
            {
                var errorMessage = $"The request: {method}:{request.Url} failed.";
                if (ex.Call.Response != null && !string.IsNullOrEmpty(ex.Call.Response.ReasonPhrase))
                {
                    errorMessage += $"Reason: {ex.Call.Response.ReasonPhrase}";
                }

                var marketplaceException = await MarketplaceException.CreateMarketplaceExceptionAsync(ex.Call.Response, errorMessage);
                _logger.Error(marketplaceException, errorMessage);
                throw marketplaceException;
            }
        }

        private IFlurlRequest CreateRequestWithHeaders(
           string requestPath,
           Guid requestId,
           Dictionary<string, string>? additionalHeaders,
           string accessToken)
        {
            var request = _endpoint
                .AppendPathSegment(requestPath)
                .SetQueryParam(DefaultApiVersionParameterName, _apiVersion)
                .WithHeaders(new
                {
                    x_ms_requestid = requestId.ToString(),
                })
                .WithOAuthBearerToken(accessToken);

            if (additionalHeaders != null)
            {
                request = request.WithHeaders(additionalHeaders);
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
                string errorMessage = $"Could not get Operation-Location header from response of request {response.RequestMessage.RequestUri}";
                throw new MarketplaceException(errorMessage);
            }
        }

        private TimeSpan GetRetryAfterValue(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter == null)
            {
                var errorMessage = $"Could not parse correct headers from operation response. Request Uri : {response.RequestMessage.RequestUri}";
                var marketplaceException = new MarketplaceException(errorMessage);
                _logger.Error(marketplaceException, errorMessage);
                throw marketplaceException;
            }

            return retryAfter.Value;
        }

        private async Task<T> PollOperationAsync<T>(IFlurlRequest originalRequest, HttpResponseMessage response, int retryCounter) where T : class
        {
            // Read all the relevant headers from the original 202 response
            var retryAfter = GetRetryAfterValue(response);
            var resultLocation = GetOperationLocationFromHeader(response);

            if (retryCounter == 0)
            {
                string errorMessage = $"Maximum retries has been reached so terminating the polling requests. Operation Id : {resultLocation}";
                var marketplaceException = await MarketplaceException.CreateMarketplaceExceptionAsync(response, errorMessage);
                _logger.Error(marketplaceException, errorMessage);
                throw marketplaceException;
            }

            // Wait as long as was requested
            await Task.Delay(retryAfter);

            var asyncOperationResponse = await GetSubrequestMessage(originalRequest, resultLocation).GetAsync();

            // you will have to check here the response and if that says complete then you can proceed or "status": "InProgress" then do it again.
            if (asyncOperationResponse.StatusCode == HttpStatusCode.OK)
            {
                var asyncResponseObj = (await asyncOperationResponse.Content.ReadAsStringAsync()).FromJson<MarketplaceAsyncOperationResponse>();

                switch (asyncResponseObj.Status)
                {
                    case OperationStatus.InProgress:
                    case OperationStatus.NotStarted:
                        _logger.LogInformation($"Trying to check the status of operation again. {resultLocation}");
                        return await PollOperationAsync<T>(originalRequest, response, --retryCounter);

                    case OperationStatus.Failed:
                        string errorMessage = $"Async operation failed while polling the operation. Operation Id : {resultLocation}";
                        var marketplaceException = await MarketplaceException.CreateMarketplaceExceptionAsync(asyncOperationResponse, errorMessage);
                        _logger.Error(marketplaceException, errorMessage);
                        throw marketplaceException;

                    case OperationStatus.Succeeded:
                        var message = $"Async operation has beeen successful. Operation Id : {resultLocation}";
                        _logger.Information(message);
                        return (await asyncOperationResponse.Content.ReadAsStringAsync()).FromJson<T>();

                    default:
                        errorMessage = $"Unknown operation is detected for async polling of marketplace API. Operation Id : {resultLocation} and status : {asyncResponseObj.Status}";
                        marketplaceException = await MarketplaceException.CreateMarketplaceExceptionAsync(asyncOperationResponse, errorMessage);
                        _logger.Error(marketplaceException, errorMessage);
                        throw marketplaceException;
                }
            }
            else
            {
                string errorMessage = $"Can not get the status of operation. Operation Id : {resultLocation}";
                var marketplaceException = await MarketplaceException.CreateMarketplaceExceptionAsync(asyncOperationResponse, errorMessage);
                _logger.Error(marketplaceException, errorMessage);
                throw marketplaceException;
            }
        }

        private static IFlurlRequest GetSubrequestMessage(IFlurlRequest originalRequest, Uri resultLocation)
        {
            return resultLocation.ToString().WithHeaders(originalRequest.Headers);
        }
#nullable disable
    }
}
