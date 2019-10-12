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

                if (!string.IsNullOrEmpty(CallContextHolder.RequestCorrelationId.Value))
                {
                    request.Headers.Add(HeaderConstants.LiftrRequestCorrelationId, CallContextHolder.RequestCorrelationId.Value);
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
