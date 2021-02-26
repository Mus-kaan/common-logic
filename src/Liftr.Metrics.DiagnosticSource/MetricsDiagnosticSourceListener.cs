//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Logging.Metrics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;

namespace Microsoft.Liftr.Metrics.DiagnosticSource
{
    /// <summary>
    /// Diagnostic listener implementation that listens for events specific to outgoing dependency requests and send metrics to geneva
    /// https://github.com/microsoft/ApplicationInsights-dotnet-server/blob/055bcaaec7249cf91ca3e1e59e8bcc08393e10e7/Src/DependencyCollector/Shared/HttpCoreDiagnosticSourceListener.cs
    /// https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/HttpDiagnosticsGuide.md
    /// </summary>
    internal sealed class MetricsDiagnosticSourceListener : IObserver<KeyValuePair<string, object>>
    {
        private readonly IMetricSender _metricSender;

        private readonly ILogger _logger;

        private readonly PropertyFetcher _startResponseFetcher = new PropertyFetcher("Response");

        public MetricsDiagnosticSourceListener(IMetricSender metricSender, ILogger logger)
        {
            _metricSender = metricSender;
            _logger = logger;
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

        public void OnNext(KeyValuePair<string, object> value)
        {
            try
            {
                if (value.Key != HttpCoreConstants.HttpOutStopEventName)
                {
                    return;
                }

                var response = _startResponseFetcher.Fetch(value.Value) as HttpResponseMessage;
                Activity activity = Activity.Current;
                var duration = activity.Duration.TotalMilliseconds;
                string httpVerb = response.RequestMessage.Method.ToString();
                string statusCode = response.StatusCode.ToString();
                IDictionary<string, string> dimensions = new Dictionary<string, string>
                {
                    ["HTTPVerb"] = httpVerb,
                    ["StatusCode"] = statusCode,
                };

                var metricType = response.RequestMessage.Headers.TryGetValues(MetricConstants.LiftrMetricTypeHeaderKey, out var values) ? values.FirstOrDefault() : string.Empty;

                // Send Marketplace Metrics. TODO - Marketplace saas status fail/success
                if (metricType.Equals(MetricConstants.MarketplaceMetricType, System.StringComparison.Ordinal))
                {
                    _metricSender.Gauge(MetricConstants.HTTPVerb_Marketplace_Duration, (int)duration, (Dictionary<string, string>)dimensions);
                    _metricSender.Gauge(MetricConstants.HTTPVerb_Marketplace_Result, 1, (Dictionary<string, string>)dimensions);
                    return;
                }

                // Send MetaRP Metrics
                if (metricType.Equals(MetricConstants.MetaRPMetricType, System.StringComparison.Ordinal))
                {
                    _metricSender.Gauge(MetricConstants.HTTPVerb_MetaRP_Duration, (int)duration, (Dictionary<string, string>)dimensions);
                    _metricSender.Gauge(MetricConstants.HTTPVerb_MetaRP_Result, 1, (Dictionary<string, string>)dimensions);
                    return;
                }

                // Send Partner Metrics
                if (metricType.Equals(MetricConstants.PartnerMetricType, System.StringComparison.Ordinal))
                {
                    _metricSender.Gauge(MetricConstants.HTTPVerb_PartnerAPI_Duration, (int)duration, (Dictionary<string, string>)dimensions);
                    _metricSender.Gauge(MetricConstants.HTTPVerb_PartnerAPI_Result, 1, (Dictionary<string, string>)dimensions);
                    return;
                }

                // Send All Other Outgoing Call Metrics
                _metricSender.Gauge(MetricConstants.HTTPVerb_DefaultCalls_Duration, (int)duration, (Dictionary<string, string>)dimensions);
                _metricSender.Gauge(MetricConstants.HTTPVerb_DefaultCalls_Result, 1, (Dictionary<string, string>)dimensions);
                _logger.Debug("Sent metris with dimension {dimension} ", dimensions.Values);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
            }
        }
    }
}
