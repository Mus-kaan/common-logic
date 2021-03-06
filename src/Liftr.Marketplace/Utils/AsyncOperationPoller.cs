//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Contracts;
using Microsoft.Liftr.Marketplace.Exceptions;
using Serilog;
using System;
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

            // Read all the relevant headers from the original 202 response
            _retryAfter = originalResponse.GetRetryAfterValue(logger);
            _operationLocation = originalResponse.GetOperationLocationFromHeader(logger);
        }

        /// <summary>
        /// Marketplace API for Polling Operation Status
        /// This polling calls the GetOperation API on <see href="https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.StoreApi%2FControllers%2FSaasV2%2FSubscriptionResourceV2Controller.cs">Marketplace side</see>
        /// </summary>
        public async Task<T> PollOperationAsync<T>(int maxRetries) where T : BaseOperationResponse
        {
            string errorMessage;
            for (int retryCount = 1; retryCount <= maxRetries; retryCount++)
            {
                await Task.Delay(_retryAfter);

                using var request = GetSubrequestMessage();
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw await MarketplaceHttpException.CreateRequestFailedExceptionAsync(response);
                }

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    errorMessage = $"Cannot get the status of polling operation for SAAS resource operation. Operation Id : {_operationLocation} Status Code: {response.StatusCode}";
                    var marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(response, errorMessage);
                    _logger.Error(marketplaceException, errorMessage);
                    throw marketplaceException;
                }

                var asyncResponseObj = (await response.Content.ReadAsStringAsync()).FromJson<BaseOperationResponse>();

                switch (asyncResponseObj.Status)
                {
                    case OperationStatus.InProgress:
                    case OperationStatus.NotStarted:
                        {
                            _logger.Information($"Trying to check the status of operation again. {_operationLocation}.Operation status for SAAS resource for Original HTTP request {_originalRequest.Method} is {asyncResponseObj.Status}");
                            continue;
                        }

                    case OperationStatus.Failed:
                        {
                            // to do: Add the error message from the asyncresponse into the exception
                            errorMessage = $"Async polling operation failed while polling the operation. Operation Id : {_operationLocation} for SAAS resource for original HTTP request {_originalRequest.Method}.";
                            var marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(response, errorMessage);
                            _logger.Error(marketplaceException, errorMessage);
                            throw marketplaceException;
                        }

                    case OperationStatus.Succeeded:
                        {
                            var message = $"Async polling operation has been successful for SAAS resource for original HTTP request {_originalRequest.Method}. Operation Id : {_operationLocation}";
                            _logger.Information(message);
                            return (await response.Content.ReadAsStringAsync()).FromJson<T>();
                        }

                    default:
                        {
                            errorMessage = $"Unknown operation is detected for async polling of marketplace API for SAAS resource for original HTTP request {_originalRequest.Method}. Operation Id : {_operationLocation} and status : {asyncResponseObj.Status}";
                            var marketplaceException = await MarketplaceHttpException.CreateMarketplaceHttpExceptionAsync(response, errorMessage);
                            _logger.Error(marketplaceException, errorMessage);
                            throw marketplaceException;
                        }
                }
            }

            errorMessage = $"Maximum retries of async polling for SAAS operation has been reached. So terminating the polling requests. Operation Id : {_operationLocation}";
            var terminalException = new MarketplaceTerminalException(errorMessage);
            _logger.Error(terminalException, errorMessage);
            throw terminalException;
        }

        private HttpRequestMessage GetSubrequestMessage()
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
