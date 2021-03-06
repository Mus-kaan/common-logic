//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Microsoft.Liftr.Logging.AspNetCore
{
    internal class LoggingMiddlewareStartupFilter : IStartupFilter
    {
        private readonly bool _logRequest;
        private readonly bool _logSubdomain;
        private readonly bool _logHostName;

        public LoggingMiddlewareStartupFilter(bool logRequest, bool logSubdomain, bool logHostName)
        {
            _logRequest = logRequest;
            _logSubdomain = logSubdomain;
            _logHostName = logHostName;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // Configure middleware
                app.UseMiddleware<LoggingMiddleware>(_logRequest, _logSubdomain, _logHostName);

                // Call the next configure method
                next(app);
            };
        }
    }
}
