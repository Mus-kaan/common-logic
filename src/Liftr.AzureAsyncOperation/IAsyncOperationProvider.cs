//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.AzureAsyncOperation
{
    public interface IAsyncOperationProvider
    {
        /// <summary>
        /// Creates azure async operation, this will set required headers as well for async operations
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="provider"></param>
        /// <param name="location"></param>
        /// <param name="httpContext"></param>
        /// <param name="retryAfter"></param>
        /// <param name="timeout"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        Task<AsyncOperationStatusEntity> CreateAsync(
            string subscriptionId,
            string provider,
            string location,
            HttpContext httpContext,
            int retryAfter,
            TimeSpan timeout,
            string errorCode = null,
            string errorMessage = null);
    }
}
