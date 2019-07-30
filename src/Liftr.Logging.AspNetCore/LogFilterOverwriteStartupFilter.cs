//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Microsoft.Liftr.Logging.AspNetCore
{
    internal class LogFilterOverwriteStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // Configure middleware
                app.UseMiddleware<LogFilterOverwriteMiddleware>();

                // Call the next configure method
                next(app);
            };
        }
    }
}
