//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.AspNetCore")]

namespace Microsoft.Liftr.DiagnosticSource
{
    /// <summary>
    /// Diagnostic listener implementation that listens for events specific to outgoing dependency requests.
    /// https://github.com/microsoft/ApplicationInsights-dotnet-server/blob/055bcaaec7249cf91ca3e1e59e8bcc08393e10e7/Src/DependencyCollector/Shared/HttpCoreDiagnosticSourceListener.cs
    /// https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/HttpDiagnosticsGuide.md
    /// </summary>
    internal sealed class HttpCoreDiagnosticSourceSubscriber : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly IDisposable _listenerSubscription;
        private readonly HttpCoreDiagnosticSourceListener _listener;
        private IDisposable _eventSubscription;

        public HttpCoreDiagnosticSourceSubscriber(HttpCoreDiagnosticSourceListener listener)
        {
            _listener = listener;
            try
            {
                _listenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            _eventSubscription?.Dispose();
            _listenerSubscription?.Dispose();
        }

        /// <summary>
        /// This method gets called once for each existing DiagnosticListener when this
        /// DiagnosticListener is added to the list of DiagnosticListeners
        /// (<see cref="System.Diagnostics.DiagnosticListener.AllListeners"/>). This method will
        /// also be called for each subsequent DiagnosticListener that is added to the list of
        /// DiagnosticListeners.
        /// <seealso cref="IObserver{T}.OnNext(T)"/>
        /// </summary>
        /// <param name="value">The DiagnosticListener that exists when this listener was added to
        /// the list, or a DiagnosticListener that got added after this listener was added.</param>
        public void OnNext(DiagnosticListener value)
        {
            if (value != null)
            {
                // Comes from https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandlerLoggingStrings.cs#L12
                if (value.Name == HttpCoreConstants.HttpHandlerDiagnosticListener)
                {
                    _eventSubscription = value.Subscribe(
                        _listener,
                        (evnt, r, _) =>
                        {
                            if (evnt == HttpCoreConstants.HttpOutStartEventName)
                            {
                                return true;
                            }

                            return false;
                        });
                }
            }
        }

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
    }
}
