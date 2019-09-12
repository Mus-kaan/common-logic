//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Liftr.AzureAsyncOperation.DataSource;
using Microsoft.Net.Http.Headers;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.Liftr.AzureAsyncOperation
{
    public class AsyncOperationProvider : IAsyncOperationProvider
    {
        private readonly IAsyncOperationStatusDataSource _asyncOperationStatusDataSource;

        public AsyncOperationProvider(IAsyncOperationStatusDataSource asyncOperationStatusDataSource)
        {
            _asyncOperationStatusDataSource = asyncOperationStatusDataSource;
        }

        public async Task<AsyncOperationStatusEntity> CreateAsync(
            string subscriptionId,
            string provider,
            string location,
            HttpContext httpContext,
            int retryAfter,
            TimeSpan timeout,
            string errorCode = null,
            string errorMessage = null)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var entity = await _asyncOperationStatusDataSource.CreateAsync(
                OperationStatus.Created,
                timeout,
                errorCode,
                errorMessage);

            // Add required headers to notify ARM about async operation
            AddResponseHeaders(httpContext, subscriptionId, provider, location, entity.Resource.OperationId, retryAfter);

            return entity;
        }

        private void AddResponseHeaders(HttpContext context, string subscriptionId, string provider, string location, string operationId, int retryAfter)
        {
            var response = context.Response;
            var locationUri = string.Format(
                CultureInfo.InvariantCulture,
                Constants.AsyncOperationLocationFormat,
                context.Request.Host,
                subscriptionId,
                provider,
                location,
                operationId,
                context.Request.Query["api-version"]);
            response.Headers.Add(Constants.AsyncOperationHeader, locationUri);
            response.Headers.Add(HeaderNames.RetryAfter, retryAfter + string.Empty);
            response.StatusCode = StatusCodes.Status202Accepted;
        }
    }
}
