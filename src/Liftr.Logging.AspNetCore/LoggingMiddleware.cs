﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

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
        private const string s_logLevelOverwriteQueryName = "LiftrLogFilterOverwrite";
        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger _logger;

        public LoggingMiddleware(RequestDelegate next, Serilog.ILogger logger)
        {
            _next = next;
            _logger = logger;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Middleware should fail silently.")]
        public async Task InvokeAsync(HttpContext httpContext)
        {
            LogEventLevel? overrideLevel = null;

            string levelOverwrite = GetHeaderValue(httpContext, HeaderConstants.LiftrLogLevelOverwrite);
            string clientRequestId = GetHeaderValue(httpContext, HeaderConstants.LiftrClientRequestId);
            string armRequestTrackingId = GetHeaderValue(httpContext, HeaderConstants.LiftrARMRequestTrackingId);
            string crrelationtId = GetHeaderValue(httpContext, HeaderConstants.LiftrRequestCorrelationId);

            if (string.IsNullOrEmpty(clientRequestId))
            {
                clientRequestId = GetHeaderValue(httpContext, HeaderConstants.ClientRequestId);
            }

            if (string.IsNullOrEmpty(armRequestTrackingId))
            {
                armRequestTrackingId = GetHeaderValue(httpContext, HeaderConstants.ARMRequestTrackingId);
            }

            if (string.IsNullOrEmpty(crrelationtId))
            {
                crrelationtId = GetHeaderValue(httpContext, HeaderConstants.RequestCorrelationId);
            }

            if (string.IsNullOrEmpty(crrelationtId))
            {
                crrelationtId = "liftr-" + Guid.NewGuid().ToString();
            }

            try
            {
                // The vaule can be set from query parameter.
                if (httpContext.Request.Query.TryGetValue(s_logLevelOverwriteQueryName, out var parameterValue))
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

            if (!string.IsNullOrEmpty(crrelationtId))
            {
                CallContextHolder.CorrelationId.Value = crrelationtId;
            }

            using (var logFilterOverrideScope = new LogFilterOverrideScope(overrideLevel))
            using (new LogContextPropertyScope("LiftrTrackingId", armRequestTrackingId))
            using (new LogContextPropertyScope("LiftrCorrelationId", crrelationtId))
            {
                await _next(httpContext);
                if (httpContext.Response?.StatusCode == (int)HttpStatusCode.NotFound && httpContext.Request?.Path.Value?.OrdinalStartsWith("/api/liveness-probe") == true)
                {
                    var meta = await _logger.GetMetaInfoAsync();
                    httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                    await httpContext.Response.WriteAsync(meta.ToJson(indented: true));
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
