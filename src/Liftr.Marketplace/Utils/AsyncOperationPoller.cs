//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Contracts;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Utils
{
    internal class AsyncOperationPoller
    {
        private readonly TimeSpan _retryAfter;
        private readonly Uri _operationLocation;
        private readonly HttpRequestMessage _originalRequest;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public AsyncOperationPoller(
            HttpRequestMessage originalRequest,
            HttpResponseMessage originalResponse,
            HttpClient httpClient,
            ILogger logger)
        {
            _originalRequest = originalRequest ?? throw new ArgumentNullException(nameof(originalRequest));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                // Read all the relevant headers from the original 202 response
                _retryAfter = originalResponse.GetRetryAfterValue(logger);
                _operationLocation = originalResponse.GetOperationLocationFromHeader(logger);
            }
            catch (InvalidOperationException ex)
            {
                var errorMessage = $"Cannot start PollingOperation due to insufficient headers. Error: {ex.Message}";
                var pollingException = PollingExceptionHelper.CreatePollingException(_originalRequest, _operationLocation, errorMessage);
                _logger.Error(pollingException.Message);
                throw pollingException;
            }
        }

        /// <summary>
        /// Marketplace API for Polling Operation Status
        /// This polling calls the GetOperation API on <see href="https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.StoreApi%2FControllers%2FSaasV2%2FSubscriptionResourceV2Controller.cs">Marketplace side</see>
        /// </summary>
        public async Task<T> PollOperationAsync<T>(int maxRetries) where T : BaseOperationResponse
        {
            string errorMessage;
            HttpStatusCode[] transientErrorHttpStatusCodes = GetTransientErrorHttpStatusCodesWorthRetrying();

            for (int retryCount = 1; retryCount <= maxRetries; retryCount++)
            {
                await Task.Delay(_retryAfter);

                using var request = CreatePollingRequest();
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    errorMessage = $"Async Polling failed with StatusCode: {response.StatusCode}.";
                    if (transientErrorHttpStatusCodes.Contains(response.StatusCode))
                    {
                        _logger.Information($"{errorMessage} Trying again.");
                        continue;
                    }

                    var ex = await PollingExceptionHelper.CreatePollingExceptionForFailResponseAsync(_originalRequest, _operationLocation, errorMessage, response);
                    _logger.Error(ex.Message);
                    throw ex;
                }

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    errorMessage = $"Async Polling received Response with StatusCode: {response.StatusCode}. This is unexpected and cannot be handled.";
                    var ex = await PollingExceptionHelper.CreatePollingExceptionForFailResponseAsync(_originalRequest, _operationLocation, errorMessage, response);
                    _logger.Error(ex.Message);
                    throw ex;
                }

                var asyncResponseObj = (await response.Content.ReadAsStringAsync()).FromJson<BaseOperationResponse>();

                switch (asyncResponseObj.Status)
                {
                    case OperationStatus.InProgress:
                    case OperationStatus.NotStarted:
                        {
                            _logger.Information($"Trying to check the status of operation again. {_operationLocation}. Operation status for SAAS resource for Original HTTP request {_originalRequest.Method} is {asyncResponseObj.Status}");
                            continue;
                        }

                    case OperationStatus.Failed:
                        {
                            if (PurchaseExceptionHelper.TryGetPurchaseFailure(asyncResponseObj.ErrorMessage, _operationLocation, _originalRequest, response, out var purchaseException))
                            {
                                throw purchaseException;
                            }
                            else
                            {
                                var ex = await PollingExceptionHelper.CreatePollingExceptionForFailResponseAsync(_originalRequest, _operationLocation, asyncResponseObj.ErrorMessage, response);
                                _logger.Error(ex.Message);
                                throw ex;
                            }
                        }

                    case OperationStatus.Succeeded:
                        {
                            var message = $"SAAS Async Polling operation has been successful for SAAS resource for original HTTP request {_originalRequest.Method}. Operation Id : {_operationLocation}";
                            _logger.Information(message);
                            return (await response.Content.ReadAsStringAsync()).FromJson<T>();
                        }

                    default:
                        {
                            errorMessage = $"SAAS Async Polling received response OperationStatus:{asyncResponseObj.Status}. This is unexpected and cannot be handled.";
                            var ex = await PollingExceptionHelper.CreatePollingExceptionForFailResponseAsync(_originalRequest, _operationLocation, errorMessage, response);
                            _logger.Error(ex.Message);
                            throw ex;
                        }
                }
            }

            {
                errorMessage = $"Maximum retries of async polling reached for SAAS resource";
                var ex = PollingExceptionHelper.CreatePollingException(_originalRequest, _operationLocation, errorMessage);
                _logger.Error(ex.Message);
                throw ex;
            }
        }

        private static HttpStatusCode[] GetTransientErrorHttpStatusCodesWorthRetrying()
        {
            return new HttpStatusCode[]
            {
                HttpStatusCode.BadGateway, // 502
                HttpStatusCode.ServiceUnavailable, // 503
                HttpStatusCode.GatewayTimeout, // 504
            };
        }

        private HttpRequestMessage CreatePollingRequest()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _operationLocation);

            foreach (var header in _originalRequest.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            return request;
        }
    }
}
