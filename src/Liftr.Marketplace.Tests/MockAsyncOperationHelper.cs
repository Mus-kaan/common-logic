//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Contracts;
using System;
using System.Net;
using System.Net.Http;

namespace Microsoft.Liftr.Marketplace.Tests
{
    internal class MockAsyncOperationHelper
    {
        public static HttpResponseMessage AcceptedResponseWithOperationLocation(string operationLocation)
        {
            var asyncOperation = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.Accepted,
            };

            asyncOperation.Headers.Add("Operation-Location", operationLocation);
            asyncOperation.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(new TimeSpan(0, 0, 1));
            return asyncOperation;
        }

        public static HttpResponseMessage SuccessResponseWithInProgressStatus()
        {
            var response = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(new MarketplaceAsyncOperationResponse() { Status = OperationStatus.InProgress }.ToJson()),
            };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(new TimeSpan(0, 0, 1));

            return response;
        }

        public static HttpResponseMessage SuccessResponseWithSucceededStatus<T>(T body) where T : MarketplaceAsyncOperationResponse
        {
            body.Status = OperationStatus.Succeeded;

            var response = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(body.ToJson()),
            };

            return response;
        }
    }
}
