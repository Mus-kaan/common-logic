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

        public LoggingMiddlewareStartupFilter(bool logRequest)
        {
            _logRequest = logRequest;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // Configure middleware
                app.UseMiddleware<LoggingMiddleware>(_logRequest);

                // Call the next configure method
                next(app);
            };
        }
    }
}
