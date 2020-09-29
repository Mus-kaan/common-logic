//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Configuration;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Logging.Formatter;
using Microsoft.Liftr.Utilities;
using Serilog;
using System;

namespace Microsoft.Liftr.Logging.GenericHosting
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Middleware should fail silently.")]
    public static class GenericHostBuilderExtension
    {
        public static IHostBuilder UseLiftrLogger(this IHostBuilder builder)
        {
            // https://andrewlock.net/adding-serilog-to-the-asp-net-core-generic-host/
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder
                .ConfigureServices((hostContext, services) =>
                {
                    LoggerExtensions.Options = hostContext.Configuration.ExtractLoggingOptions();
                    var options = LoggerExtensions.Options;

                    var serilogConfig = new LoggerConfiguration();
                    serilogConfig = serilogConfig.ReadFrom.Configuration(hostContext.Configuration);

                    if (options.AllowFilterDynamicOverride)
                    {
                        serilogConfig = serilogConfig.MinimumLevel.ControlledBy(LogFilterOverrideScope.EnableFilterOverride(options.MinimumLevel));
                    }

                    var ikey = options.AppInsigthsInstrumentationKey;
                    if (!string.IsNullOrEmpty(ikey))
                    {
                        var appInsightsConfig = TelemetryConfiguration.CreateDefault();
                        appInsightsConfig.InstrumentationKey = ikey;
                        DependencyTrackingTelemetryModule depModule = new DependencyTrackingTelemetryModule();
                        depModule.Initialize(appInsightsConfig);
                        var builder = appInsightsConfig.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
                        builder.Use((next) => new LiftrTelemetryProcessor(next));
                        builder.Build();
                        var appInsightsClient = new TelemetryClient(appInsightsConfig);
                        AppInsightsHelper.AppInsightsClient = appInsightsClient;
                        serilogConfig = serilogConfig.WriteTo.ApplicationInsights(appInsightsClient, TelemetryConverter.Events);
                        services.AddSingleton(sp => appInsightsClient);
                        services.AddHostedService<AppInsightsFlushService>();
                    }

                    if (!hostContext.Configuration.ContainsSerilogWriteToConsole())
                    {
                        serilogConfig = serilogConfig.WriteTo.Console(new CompactJsonFormatter(renderMessage: options.RenderMessage));
                    }

                    serilogConfig = serilogConfig.Enrich.FromLogContext();

                    var meta = InstanceMetaHelper.GetMetaInfoAsync().Result;
                    var instanceMeta = meta?.InstanceMeta;
                    if (instanceMeta != null)
                    {
                        serilogConfig = serilogConfig
                        .Enrich.WithProperty("AppVer", meta.Version)
                        .Enrich.WithProperty("vmRegion", instanceMeta.Compute.Location)
                        .Enrich.WithProperty("vmName", instanceMeta.Compute.Name)
                        .Enrich.WithProperty("vmRG", instanceMeta.Compute.ResourceGroupName);
                    }

                    var logger = serilogConfig.CreateLogger();
                    Log.Logger = logger;

                    services.AddSingleton(Log.Logger);
                    services.AddSingleton(new HttpCoreDiagnosticSourceSubscriber(new HttpCoreDiagnosticSourceListener()));
                })
                .UseSerilog();
        }
    }
}
