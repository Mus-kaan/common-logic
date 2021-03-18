//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Exceptions;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Utils
{
    internal static class PollingExceptionHelper
    {
        internal static PollingException CreatePollingException(
            HttpRequestMessage originalRequest,
            Uri operationUri,
            string errorMessage)
        {
            var stringBuilder = ExceptionMessageUtils.BuildPollingFailedMessage(originalRequest, operationUri, errorMessage);
            var message = stringBuilder.ToString();
            return new PollingException(message, originalRequest, operationUri);
        }

        internal static async Task<PollingException> CreatePollingExceptionForFailResponseAsync(
            HttpRequestMessage originalRequest,
            Uri operationUri,
            string errorMessage,
            HttpResponseMessage pollingResponse)
        {
            var stringBuilder = ExceptionMessageUtils.BuildPollingFailedMessage(originalRequest, operationUri, errorMessage);
            stringBuilder.AppendLine($"Response Status: {pollingResponse.StatusCode}.");
            stringBuilder.AppendLine($"Response Reason: {pollingResponse.ReasonPhrase}.");
            if (pollingResponse.Content != null)
            {
                var content = await pollingResponse.Content.ReadAsStringAsync();
                stringBuilder.AppendLine($"Response Content: {content}.");
            }

            return new PollingException(stringBuilder.ToString(), originalRequest, operationUri, pollingResponse);
        }
    }
}
