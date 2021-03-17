//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Exceptions
{
    public static class ExceptionMessageUtils
    {
        internal static async Task<string> BuildRequestFailedMessageAsync(HttpRequestMessage request, HttpResponseMessage response)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Request failed for SAAS operation.");
            stringBuilder.AppendLine($"Request Uri: {request.RequestUri}.");
            stringBuilder.AppendLine($"Request Method: {request.Method}.");
            stringBuilder.AppendLine($"Response StatusCode: {response.StatusCode}.");

            if (response.Content != null)
            {
                var content = await response.Content.ReadAsStringAsync();
                stringBuilder.AppendLine($"Response Content: {content}");
            }

            return stringBuilder.ToString();
        }

        internal static StringBuilder BuildPollingFailedMessage(HttpRequestMessage originalRequest, Uri operationUri, string errorMessage)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Async polling operation failed for SAAS resource.");
            if (errorMessage != null)
            {
                stringBuilder.AppendLine($"ErrorMessage: {errorMessage}.");
            }

            stringBuilder.AppendLine($"Operation Uri: {operationUri}.");
            stringBuilder.AppendLine($"Original Request Uri: {originalRequest.RequestUri}.");
            stringBuilder.AppendLine($"Original Request Method: {originalRequest.Method}.");
            return stringBuilder;
        }

        internal static StringBuilder BuildPurchaseFailureMessage(HttpRequestMessage originalRequest, Uri operationUri, PurchaseErrorType errorType, string errorMessage)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Purchase operation failed for SAAS resource.");
            stringBuilder.AppendLine($"Purchase Error Type: {errorType}");
            if (errorMessage != null)
            {
                stringBuilder.AppendLine($"ErrorMessage: {errorMessage}.");
            }

            stringBuilder.AppendLine($"Operation Uri: {operationUri}.");
            stringBuilder.AppendLine($"Original Request Uri: {originalRequest.RequestUri}.");
            stringBuilder.AppendLine($"Original Request Method: {originalRequest.Method}.");
            return stringBuilder;
        }
    }
}
