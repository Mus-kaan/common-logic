//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DiagnosticSource;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                var dimensions = new Dictionary<string, string>
                {
                    ["HTTPVerb"] = httpVerb,
                    ["StatusCode"] = statusCode,
                };

                // Send Marketplace Metrics. TODO - Marketplace saas status fail/success
                if (!string.IsNullOrEmpty(MetricContextHolder.MarketplaceMetricContext.Value))
                {
                    _metricSender.Gauge(MetricConstants.HTTPVerb_Marketplace_Duration, (int)duration, dimensions);
                    _metricSender.Gauge(MetricConstants.HTTPVerb_Marketplace_Result, 1, dimensions);
                    _logger.Debug("Sent marketplace metrics with dimensions {dimensions} ", dimensions.Values);
                    return;
                }

                // Send MetaRP Metrics
                if (!string.IsNullOrEmpty(MetricContextHolder.MetaRPMetricContext.Value))
                {
                    _metricSender.Gauge(MetricConstants.HTTPVerb_MetaRP_Duration, (int)duration, dimensions);
                    _metricSender.Gauge(MetricConstants.HTTPVerb_MetaRP_Result, 1, dimensions);
                    _logger.Debug("Sent metarp metrics with dimensions {dimensions} ", dimensions.Values);
                    return;
                }

                // Send Partner Metrics
                if (!string.IsNullOrEmpty(MetricContextHolder.PartnerMetricContext.Value))
                {
                    _metricSender.Gauge(MetricConstants.HTTPVerb_PartnerAPI_Duration, (int)duration, dimensions);
                    _metricSender.Gauge(MetricConstants.HTTPVerb_PartnerAPI_Result, 1, dimensions);
                    _logger.Debug("Sent partner metrics with dimensions {dimensions} ", dimensions.Values);
                    return;
                }

                // Send All Other Outgoing Call Metrics
                _metricSender.Gauge(MetricConstants.HTTPVerb_DefaultCalls_Duration, (int)duration, dimensions);
                _metricSender.Gauge(MetricConstants.HTTPVerb_DefaultCalls_Result, 1, dimensions);
                _logger.Debug("Sent metrics with dimension {dimensions} ", dimensions.Values);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
            }
        }
    }
}
