//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Utilities;
using Serilog.Events;
using System;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.AspNetCore.Tests")]

namespace Microsoft.Liftr.Logging.AspNetCore
{
    internal class LoggingMiddleware
    {
        private const string c_logLevelOverwriteQueryName = "LiftrLogFilterOverwrite";

        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger _logger;
        private readonly bool _logRequest;
        private readonly bool _logHostName;
        private readonly bool _logSubdomain;

        public LoggingMiddleware(RequestDelegate next, Serilog.ILogger logger, bool logRequest, bool logSubdomain, bool logHostName)
        {
            _next = next;
            _logger = logger;
            _logRequest = logRequest;
            _logSubdomain = logSubdomain;
            _logHostName = logHostName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Middleware should fail silently.")]
        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (httpContext.Request?.Path.Value?.OrdinalStartsWith("/api/liveness-probe") == true ||
                httpContext.Request?.Path.Value?.OrdinalStartsWith("/metrics") == true)
            {
                try
                {
                    // This is to remove AppInsights logging when it is enabled. If not, this will do nothing.
                    httpContext?.Features?.Set<RequestTelemetry>(null);
                }
                catch
                {
                }
            }

            if (httpContext.Request?.Path.Value?.OrdinalStartsWith("/api/liveness-probe") == true)
            {
                var meta = await InstanceMetaHelper.GetMetaInfoAsync();
                httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                if (meta != null)
                {
                    var pingResult = LivenessPingResult.FromMetaInfo(meta);
                    await httpContext.Response.WriteAsync(pingResult.ToJson(indented: true));
                }

                return;
            }

            if (httpContext.Request?.Path.Value?.OrdinalStartsWith("/metrics") == true)
            {
                await _next(httpContext);
                return;
            }

            LogEventLevel? overrideLevel = null;

            // Get trace context from Liftr headers.
            string levelOverwrite = GetHeaderValue(httpContext, HeaderConstants.LiftrLogLevelOverwrite);
            string clientRequestId = GetHeaderValue(httpContext, HeaderConstants.LiftrClientRequestId);
            string armRequestTrackingId = GetHeaderValue(httpContext, HeaderConstants.LiftrARMRequestTrackingId);
            string correlationtId = GetHeaderValue(httpContext, HeaderConstants.LiftrRequestCorrelationId);

            // Get trace context from Microsoft public documented headers.
            if (string.IsNullOrEmpty(clientRequestId))
            {
                clientRequestId = GetHeaderValue(httpContext, HeaderConstants.ARMClientRequestId);
            }

            if (string.IsNullOrEmpty(clientRequestId))
            {
                clientRequestId = GetHeaderValue(httpContext, HeaderConstants.MarketplaceRequestId);
            }

            if (string.IsNullOrEmpty(armRequestTrackingId))
            {
                armRequestTrackingId = GetHeaderValue(httpContext, HeaderConstants.ARMRequestTrackingId);
            }

            if (string.IsNullOrEmpty(correlationtId))
            {
                correlationtId = GetHeaderValue(httpContext, HeaderConstants.RequestCorrelationId);
            }

            if (string.IsNullOrEmpty(correlationtId))
            {
                correlationtId = GetHeaderValue(httpContext, HeaderConstants.MarketplaceCorrelationId);
            }

            // Set default trace context.
            if (string.IsNullOrEmpty(correlationtId))
            {
                correlationtId = Guid.NewGuid().ToString();
            }

            try
            {
                // The vaule can be set from query parameter.
                if (httpContext?.Request?.Query?.TryGetValue(c_logLevelOverwriteQueryName, out var parameterValue) == true)
                {
                    levelOverwrite = parameterValue.LastOrDefault();
                }
            }
            catch
            {
            }

            try
            {
                if (!string.IsNullOrEmpty(levelOverwrite))
                {
                    if (Enum.TryParse<LogEventLevel>(levelOverwrite, true, out var level))
                    {
                        overrideLevel = level;

                        // Pass the log level filter to the next tier.
                        CallContextHolder.LogFilterOverwrite.Value = levelOverwrite;
                    }
                }
            }
            catch
            {
            }

            if (!string.IsNullOrEmpty(clientRequestId))
            {
                CallContextHolder.ClientRequestId.Value = clientRequestId;
            }

            if (!string.IsNullOrEmpty(armRequestTrackingId))
            {
                CallContextHolder.ARMRequestTrackingId.Value = armRequestTrackingId;
            }

            if (!string.IsNullOrEmpty(correlationtId))
            {
                CallContextHolder.CorrelationId.Value = correlationtId;
            }

            var subdomain = string.Empty;
            var hostName = httpContext?.Request?.Host.Value;
            if (_logSubdomain && !string.IsNullOrWhiteSpace(hostName))
            {
                subdomain = hostName.Split('.')[0];
            }

            using (var logFilterOverrideScope = new LogFilterOverrideScope(overrideLevel))
            using (new LogContextPropertyScope("LiftrClientReqId", clientRequestId))
            using (new LogContextPropertyScope("LiftrTrackingId", armRequestTrackingId))
            using (new LogContextPropertyScope("LiftrCorrelationId", correlationtId))
            using (new LogContextPropertyScope("HostName", _logHostName ? hostName : string.Empty))
            using (new LogContextPropertyScope("Subdomain", subdomain))
            {
                var scope = new RequestLoggingScope(httpContext, _logger, _logRequest, correlationtId);
                try
                {
                    await _next(httpContext);
                    scope.Finish(httpContext);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unhandled exception.");
                    scope.Finish(httpContext, ex);
                    throw;
                }
            }
        }

        private static string GetHeaderValue(HttpContext httpContext, string headerName)
        {
            if (httpContext?.Request?.Headers?.TryGetValue(headerName, out var headerValue) == true)
            {
                return headerValue.FirstOrDefault() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
