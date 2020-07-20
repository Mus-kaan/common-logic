//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Liftr.DiagnosticSource;
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

        public LoggingMiddleware(RequestDelegate next, Serilog.ILogger logger, bool logRequest)
        {
            _next = next;
            _logger = logger;
            _logRequest = logRequest;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Middleware should fail silently.")]
        public async Task InvokeAsync(HttpContext httpContext)
        {
            LogEventLevel? overrideLevel = null;

            string levelOverwrite = GetHeaderValue(httpContext, HeaderConstants.LiftrLogLevelOverwrite);
            string clientRequestId = GetHeaderValue(httpContext, HeaderConstants.LiftrClientRequestId);
            string armRequestTrackingId = GetHeaderValue(httpContext, HeaderConstants.LiftrARMRequestTrackingId);
            string correlationtId = GetHeaderValue(httpContext, HeaderConstants.LiftrRequestCorrelationId);

            if (string.IsNullOrEmpty(clientRequestId))
            {
                clientRequestId = GetHeaderValue(httpContext, HeaderConstants.ARMClientRequestId);
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

            if (httpContext.Request?.Path.Value?.OrdinalStartsWith("/api/liveness-probe") == true)
            {
                try
                {
                    // This is to remove AppInsights logging when it is enabled. If not, this will do nothing.
                    httpContext?.Features?.Set<RequestTelemetry>(null);
                }
                catch
                {
                }

                var meta = await InstanceMetaHelper.GetMetaInfoAsync();
                httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                if (meta != null)
                {
                    await httpContext.Response.WriteAsync(meta.ToJson(indented: true));
                }

                return;
            }

            using (var logFilterOverrideScope = new LogFilterOverrideScope(overrideLevel))
            using (new LogContextPropertyScope("LiftrClientReqId", clientRequestId))
            using (new LogContextPropertyScope("LiftrTrackingId", armRequestTrackingId))
            using (new LogContextPropertyScope("LiftrCorrelationId", correlationtId))
            {
                var scope = new RequestLoggingScope(httpContext?.Request, _logger, _logRequest, correlationtId);
                try
                {
                    await _next(httpContext);
                    scope.Finish(httpContext?.Response);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unhandled exception.");
                    scope.Finish(httpContext?.Response, ex);
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
