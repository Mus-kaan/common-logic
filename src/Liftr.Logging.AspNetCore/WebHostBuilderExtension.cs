//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Liftr.DiagnosticSource;
using Serilog;
using Serilog.Events;
using System;

namespace Microsoft.Liftr.Logging.AspNetCore
{
    public static class WebHostBuilderExtension
    {
        /// <summary>
        /// This will do the following:
        /// 1. Application Insights SDK is added. The instrumentation key can be configured in the appsettings.json.
        /// 2. The Serilog <see cref="Serilog.Log"/> logger will be configured according to the 'Serilog' section in the appsettings.json.
        /// 3. Minimum log filter can be override by setting a request header or query parameter and the overide value will be passed to all out-going http calls.
        /// i.e. <see cref="LogFilterOverwriteMiddleware"/> will be added to the ASP.NET core middleware pipeline before the code in the 'Startup.cs'.
        /// 4. <see cref="LogFilterOverwriteMiddleware"/> can be disabled by setting the value of 'Serilog:AllowFilterDynamicOverride' to 'false' in appsettings.json.
        /// </summary>
        /// <param name="webHostBuilder"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseLiftrLogger(this IWebHostBuilder webHostBuilder)
        {
            if (webHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(webHostBuilder));
            }

            webHostBuilder
                .UseApplicationInsights() // Cross tier correlation is added here.
                .UseSerilog((host, config) =>
                {
                    (var allowOverride, var defaultLevel) = GetOverrideOptions(host);
                    if (allowOverride)
                    {
                        config.ReadFrom.Configuration(host.Configuration).MinimumLevel.ControlledBy(LogFilterOverrideScope.EnableFilterOverride(defaultLevel));
                    }
                    else
                    {
                        config.ReadFrom.Configuration(host.Configuration);
                    }
                })
                .ConfigureServices((host, services) =>
                {
                    (var allowOverride, var defaultLevel) = GetOverrideOptions(host);
                    if (allowOverride)
                    {
                        var subscriber = new HttpCoreDiagnosticSourceSubscriber(new HttpCoreDiagnosticSourceListener());
                        services.AddSingleton<IStartupFilter, LogFilterOverwriteStartupFilter>();
                        services.AddSingleton(subscriber);
                    }

                    services.AddSingleton(Log.Logger);
                });

            return webHostBuilder;
        }

        private static (bool allowOverride, LogEventLevel defaultLevel) GetOverrideOptions(WebHostBuilderContext host)
        {
            bool allowOverride = true;
            try
            {
                var allowOverrideStr = host.Configuration.GetSection("Serilog")?.GetSection("AllowFilterDynamicOverride")?.Value;
                allowOverride = bool.Parse(allowOverrideStr);
            }
            catch
            {
            }

            LogEventLevel defaultLevel = LogEventLevel.Information;
            try
            {
                var defaultLevelStr = host.Configuration.GetSection("Serilog")?.GetSection("MinimumLevel")?.GetSection("Default")?.Value;
                defaultLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), defaultLevelStr);
            }
            catch
            {
            }

            return (allowOverride, defaultLevel);
        }
    }
}
