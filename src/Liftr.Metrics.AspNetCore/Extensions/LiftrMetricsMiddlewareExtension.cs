//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Liftr.Metrics.AspNetCore;

namespace Microsoft.Liftr.Metrics.AspNetCore.Extensions
{
    public static class LiftrMetricsMiddlewareExtension
    {
        public static IApplicationBuilder UseLiftrMetricsMiddleware(this IApplicationBuilder builder, string serviceName)
        {
            return builder.UseMiddleware<LiftrMetricsMiddleware>(serviceName);
        }
    }
}
