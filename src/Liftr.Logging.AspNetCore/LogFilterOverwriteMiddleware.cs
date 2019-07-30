//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Liftr.DiagnosticSource;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.AspNetCore.Tests")]

namespace Microsoft.Liftr.Logging.AspNetCore
{
    internal class LogFilterOverwriteMiddleware
    {
        private const string s_logLevelOverwriteHeaderName = "X-LIFTR-LOG-FILTER-OVERWRITE";
        private const string s_logLevelOverwriteQueryName = "LiftrLogFilterOverwrite";
        private readonly RequestDelegate _next;

        public LogFilterOverwriteMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            LogEventLevel? overrideLevel = null;
            string levelOverwrite = string.Empty;
            try
            {
                // The vaule can be set from http header.
                if (httpContext.Request.Headers.TryGetValue(s_logLevelOverwriteHeaderName, out var headerValue))
                {
                    levelOverwrite = headerValue.LastOrDefault();
                }
            }
            catch
            {
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
                        if (CallContextHolder.CommonHttpHeaders.Value == null)
                        {
                            CallContextHolder.CommonHttpHeaders.Value = new Dictionary<string, string>();
                        }

                        CallContextHolder.CommonHttpHeaders.Value[s_logLevelOverwriteHeaderName] = levelOverwrite;
                    }
                }
            }
            catch
            {
            }

            using (var logFilterOverrideScope = new LogFilterOverrideScope(overrideLevel))
            {
                await _next(httpContext);
            }
        }
    }
}
