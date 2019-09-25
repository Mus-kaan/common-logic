//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Context;
using System;

namespace Microsoft.Liftr.Logging.GenericHosting
{
    public static class GenericHostBuilderExtension
    {
        public static IHostBuilder UseLiftrLogger(this IHostBuilder builder)
        {
            // https://andrewlock.net/adding-serilog-to-the-asp-net-core-generic-host/
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            LogContext.PushProperty("RunningSessionId", Guid.NewGuid().ToString());
            LogContext.PushProperty("ProcessStartTime", DateTime.UtcNow.ToZuluString());

            return builder
                .ConfigureServices((hostContext, services) =>
                {
                    var serilogConfig = new LoggerConfiguration();
                    serilogConfig = serilogConfig.ReadFrom.Configuration(hostContext.Configuration);

                    var ikey = hostContext.Configuration.GetSection("ApplicationInsights")?.GetSection("InstrumentationKey")?.Value;
                    if (!string.IsNullOrEmpty(ikey))
                    {
                        var appInsightsConfig = TelemetryConfiguration.CreateDefault();
                        appInsightsConfig.InstrumentationKey = ikey;
                        var appInsightsClient = new TelemetryClient(appInsightsConfig);
                        serilogConfig = serilogConfig.WriteTo.ApplicationInsights(appInsightsClient, TelemetryConverter.Events);
                        services.AddSingleton(appInsightsClient);
                        services.AddHostedService<AppInsightsFlushService>();
                    }

                    serilogConfig = serilogConfig.Enrich.FromLogContext();
                    var logger = serilogConfig.CreateLogger();
                    Log.Logger = logger;
                    Log.Information("Created Serilog logger.");

                    services.AddSingleton<Serilog.ILogger>(Log.Logger);
                    Log.Information("Serilog logger is added to DI container.");
                })
                .UseSerilog();
        }
    }
}
