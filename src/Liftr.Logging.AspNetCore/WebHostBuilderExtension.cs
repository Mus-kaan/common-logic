//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Liftr.Configuration;
using Microsoft.Liftr.DiagnosticSource;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;

namespace Microsoft.Liftr.Logging.AspNetCore
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Middleware should fail silently.")]
    public static class WebHostBuilderExtension
    {
        /// <summary>
        /// This will do the following:
        /// 1. Application Insights SDK is added. The instrumentation key can be configured in the appsettings.json.
        /// 2. The Serilog <see cref="Serilog.Log"/> logger will be configured according to the 'Serilog' section in the appsettings.json.
        /// 3. Minimum log filter can be override by setting a request header or query parameter and the overide value will be passed to all out-going http calls.
        /// i.e. <see cref="LoggingMiddleware"/> will be added to the ASP.NET core middleware pipeline before the code in the 'Startup.cs'.
        /// 4. <see cref="LoggingMiddleware"/> can be disabled by setting the value of 'Serilog:AllowFilterDynamicOverride' to 'false' in appsettings.json.
        /// </summary>
        /// <param name="webHostBuilder"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseLiftrLogger(this IWebHostBuilder webHostBuilder)
        {
            if (webHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(webHostBuilder));
            }

            LogContext.PushProperty("RunningSessionId", Guid.NewGuid().ToString());
            LogContext.PushProperty("ProcessStartTime", DateTime.UtcNow.ToZuluString());

            webHostBuilder
                .UseSerilog((host, config) =>
                {
                    (var allowOverride, var logRequest, var defaultLevel) = GetOverrideOptions(host);
                    if (allowOverride)
                    {
                        config.ReadFrom.Configuration(host.Configuration).MinimumLevel.ControlledBy(LogFilterOverrideScope.EnableFilterOverride(defaultLevel)).Enrich.FromLogContext();
                    }
                    else
                    {
                        config.ReadFrom.Configuration(host.Configuration).Enrich.FromLogContext();
                    }

                    var meta = InstanceMetaHelper.GetMetaInfoAsync().Result;
                    var instanceMeta = meta?.InstanceMeta;
                    if (instanceMeta != null)
                    {
                        config
                        .Enrich.WithProperty("AppVer", meta.Version)
                        .Enrich.WithProperty("vmRegion", instanceMeta.Compute.Location)
                        .Enrich.WithProperty("vmName", instanceMeta.Compute.Name)
                        .Enrich.WithProperty("vmRG", instanceMeta.Compute.ResourceGroupName);
                    }

                    if (!host.Configuration.ContainsSerilogWriteToConsole())
                    {
                        config = config.WriteTo.Console(new CompactJsonFormatter());
                    }
                })
                .ConfigureServices((host, services) =>
                {
                    (var allowOverride, var logRequest, var defaultLevel) = GetOverrideOptions(host);
                    LoggerExtensions.Options.LogTimedOperation = GetLogTimedOperation(host);

                    var ikey = host.Configuration.GetSection("ApplicationInsights")?.GetSection("InstrumentationKey")?.Value;
                    if (!string.IsNullOrEmpty(ikey))
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        var builder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;
#pragma warning restore CS0618 // Type or member is obsolete
                        builder.Use((next) => new NoSamplingTelemetryProcessor(next));
                        builder.Build();
                        services.AddApplicationInsightsTelemetry();
                    }

                    services.AddSingleton(new HttpCoreDiagnosticSourceSubscriber(new HttpCoreDiagnosticSourceListener()));

                    services.AddSingleton<IStartupFilter>((sp) =>
                    {
                        return new LoggingMiddlewareStartupFilter(logRequest);
                    });

                    services.AddSingleton<Serilog.ILogger>((sp) =>
                    {
                        if (!string.IsNullOrEmpty(ikey))
                        {
                            var appInsightsClient = sp.GetService<TelemetryClient>();
                            AppInsightsHelper.AppInsightsClient = appInsightsClient;
                        }

                        return Log.Logger;
                    });
                });

            return webHostBuilder;
        }

        private static (bool allowOverride, bool logRequest, LogEventLevel defaultLevel) GetOverrideOptions(WebHostBuilderContext host)
        {
            bool allowOverride = false;
            bool logRequest = false;
            try
            {
                var allowOverrideStr = host.Configuration.GetSection("Serilog")?.GetSection("AllowFilterDynamicOverride")?.Value;
                allowOverride = bool.Parse(allowOverrideStr);
            }
            catch
            {
            }

            try
            {
                var allowOverrideStr = host.Configuration.GetSection("Serilog")?.GetSection("LogRequest")?.Value;
                logRequest = bool.Parse(allowOverrideStr);
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

            return (allowOverride, logRequest, defaultLevel);
        }

        private static bool GetLogTimedOperation(WebHostBuilderContext host)
        {
            bool logTimedOperation = true;
            try
            {
                var allowOverrideStr = host.Configuration.GetSection("Serilog")?.GetSection("LogTimedOperation")?.Value;
                logTimedOperation = bool.Parse(allowOverrideStr);
            }
            catch
            {
            }

            return logTimedOperation;
        }
    }
}
