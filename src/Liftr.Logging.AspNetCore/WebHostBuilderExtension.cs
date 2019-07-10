//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

namespace Microsoft.Liftr.Logging.AspNetCore
{
    public static class WebHostBuilderExtension
    {
        public static IWebHostBuilder UseLiftrLogger(this IWebHostBuilder webHostBuilder)
        {
            if (webHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(webHostBuilder));
            }

            webHostBuilder
                .UseApplicationInsights()
                .UseSerilog((host, config) => config.ReadFrom.Configuration(host.Configuration))
                .ConfigureServices(services =>
                {
                    services.AddSingleton(Log.Logger);
                });

            return webHostBuilder;
        }
    }
}
