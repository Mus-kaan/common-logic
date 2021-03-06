//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Liftr.Logging.Metrics;
using Microsoft.Liftr.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Metrics.AspNetCore
{
    /// <summary>
    /// Metric Middleware for all the Incoming Requests
    /// </summary>
    public class LiftrMetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger _logger;
        private readonly IMetricSender _metricSender;
        private readonly string _serviceName;

        public LiftrMetricsMiddleware(RequestDelegate next, IMetricSender metricSender, Serilog.ILogger logger, string serviceName)
        {
            _next = next;
            _metricSender = metricSender;
            _logger = logger;
            _serviceName = serviceName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Middleware should fail silently.")]
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                await _next(httpContext);
                sw.Stop();

                if (httpContext.Request?.Path.Value?.OrdinalStartsWith("/api/liveness-probe") == true ||
                    httpContext.Request?.Path.Value?.OrdinalStartsWith("/metrics") == true)
                {
                    return;
                }

                var duration = sw.ElapsedMilliseconds;
                var statusCode = httpContext.Response.StatusCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string requestPath = httpContext.Request.Path;
                var dimensions = new Dictionary<string, string>()
                {
                    ["HTTPVerb"] = httpContext.Request.Method,
                    ["StatusCode"] = statusCode,
                };

                _metricSender.Gauge($"HttpVerb_{_serviceName}_Duration", (int)duration, dimensions);
                _logger.Debug("Metrics sent to geneva {dimensions}", dimensions.Values);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception during metric middleware - {message}", ex.Message);
            }
        }
    }
}
