//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Liftr.DiagnosticSource
{
    /// <summary>
    /// Diagnostic listener implementation that listens for events specific to outgoing dependency requests.
    /// https://github.com/microsoft/ApplicationInsights-dotnet-server/blob/055bcaaec7249cf91ca3e1e59e8bcc08393e10e7/Src/DependencyCollector/Shared/HttpCoreDiagnosticSourceListener.cs
    /// https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/HttpDiagnosticsGuide.md
    /// </summary>
    internal sealed class HttpCoreDiagnosticSourceListener : IObserver<KeyValuePair<string, object>>
    {
        private static readonly HashSet<string> s_domainsToAddCorrelationHeader = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // ARM endpoints
            // source: https://github.com/Azure/azure-libraries-for-net/blob/4ffeb074323fad078b6ef8823b406afdb06ef654/src/ResourceManagement/ResourceManager/AzureEnvironment.cs
            "management.azure.com",
            "api-dogfood.resources.windows-int.net",
            "management.chinacloudapi.cn",
            "management.usgovcloudapi.net",
            "management.microsoftazure.de",
        };

        private readonly PropertyFetcher _startRequestFetcher = new PropertyFetcher("Request");

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// <seealso cref="IObserver{T}.OnCompleted()"/>
        /// </summary>
        public void OnCompleted()
        {
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// <seealso cref="IObserver{T}.OnError(Exception)"/>
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// Provides the observer with new data.
        /// <seealso cref="IObserver{T}.OnNext(T)"/>
        /// </summary>
        /// <param name="value">The current notification information.</param>
        public void OnNext(KeyValuePair<string, object> value)
        {
            try
            {
                if (value.Key != HttpCoreConstants.HttpOutStartEventName)
                {
                    return;
                }

                var request = _startRequestFetcher.Fetch(value.Value) as HttpRequestMessage;

                // Inject the headers to all out-going http requests.
                if (!string.IsNullOrEmpty(CallContextHolder.LogFilterOverwrite.Value))
                {
                    request.Headers.Add(HeaderConstants.LiftrLogLevelOverwrite, CallContextHolder.LogFilterOverwrite.Value);
                }

                if (!string.IsNullOrEmpty(CallContextHolder.ClientRequestId.Value))
                {
                    request.Headers.Add(HeaderConstants.LiftrClientRequestId, CallContextHolder.ClientRequestId.Value);
                }

                if (!string.IsNullOrEmpty(CallContextHolder.ARMRequestTrackingId.Value))
                {
                    request.Headers.Add(HeaderConstants.LiftrARMRequestTrackingId, CallContextHolder.ARMRequestTrackingId.Value);
                }

                if (!string.IsNullOrEmpty(CallContextHolder.CorrelationId.Value))
                {
                    request.Headers.Add(HeaderConstants.LiftrRequestCorrelationId, CallContextHolder.CorrelationId.Value);

                    if (!request.Headers.Contains(HeaderConstants.RequestCorrelationId) &&
                        s_domainsToAddCorrelationHeader.Contains(request.RequestUri.Host))
                    {
                        // We cannot add the 'X-MS-Correlation-Request-Id' to all the outbound requests.
                        // e.g. storage requests is also using this header, and it will sign the request content (include this header)
                        // and store a signature in the request to avoid tampering. If we add this header again, the signature will not match.
                        request.Headers.Add(HeaderConstants.RequestCorrelationId, CallContextHolder.CorrelationId.Value);
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types. Override this to make sure the injection part is not affecting any application.
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
            }
        }
    }
}
