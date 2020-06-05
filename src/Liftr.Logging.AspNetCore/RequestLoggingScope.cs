//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Liftr.Contracts;
using System;

namespace Microsoft.Liftr.Logging.AspNetCore
{
    internal class RequestLoggingScope
    {
        private readonly ResourceIdPath _requestIdPath;
        private ITimedOperation _operation;

        public RequestLoggingScope(
            HttpRequest request,
            Serilog.ILogger logger,
            bool logRequest,
            string correlationtId)
        {
            if (!logRequest)
            {
                return;
            }

            var requestPath = request?.Path.Value;
            if (string.IsNullOrEmpty(requestPath))
            {
                return;
            }

            var httpMethod = request?.Method?.ToUpperInvariant();
            if (string.IsNullOrEmpty(httpMethod))
            {
                return;
            }

            if (ResourceIdPath.TryParse(requestPath, out var requestIdPath))
            {
                // This is removing the changing part from the path. The resulting path will not have the dynamic parts, e.g:
                // 'PUT /subscriptions/<subscriptionId>/resourceGroups/<resourceGroup>/providers/Microsoft.Datadog/monitors/<name>'
                _requestIdPath = requestIdPath;
                var operationName = $"{httpMethod} {requestIdPath.GenericPath}";
                _operation = logger.StartTimedOperation(operationName, correlationtId);
                _operation.SetContextProperty("subId", requestIdPath.ResourceId.SubscriptionId);
                _operation.SetContextProperty("rg", requestIdPath.ResourceId.ResourceGroup);

                if (!string.IsNullOrEmpty(requestIdPath.ResourceId.ResourceName))
                {
                    _operation.SetContextProperty(requestIdPath.ResourceId.ResourceType, requestIdPath.ResourceId.ResourceName);
                }

                if (!string.IsNullOrEmpty(requestIdPath.ResourceId.ChildResourceName))
                {
                    _operation.SetContextProperty(requestIdPath.ResourceId.ChildResourceType, requestIdPath.ResourceId.ChildResourceName);
                }
            }
            else
            {
                var operationName = $"{httpMethod} {requestPath}";
                _operation = logger.StartTimedOperation(operationName, correlationtId);
            }
        }

        public void Finish(HttpResponse response, Exception ex = null)
        {
            if (_operation != null)
            {
                if (ex == null)
                {
                    _operation.SetResult(response.StatusCode);
                }
                else
                {
                    _operation.FailOperation(ex.Message);
                }

                _operation.Dispose();
                _operation = null;
            }
        }
    }
}
