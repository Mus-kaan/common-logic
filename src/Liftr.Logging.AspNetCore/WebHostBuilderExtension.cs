//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Destructurama;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Liftr.Configuration;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Logging.Contracts;
using Microsoft.Liftr.Logging.Formatter;
using Microsoft.Liftr.Utilities;
using Serilog;
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

            var meta = InstanceMetaHelper.GetMetaInfoAsync().Result;
            var instanceMeta = meta?.InstanceMeta;
            var vmRegion = instanceMeta?.Compute?.Location;
            var liftrTelemetryInitializer = new LiftrTelemetryInitializer(vmRegion);

            webHostBuilder
                .UseSerilog((host, config) =>
                {
                    LoggerExtensions.Options = host.Configuration.ExtractLoggingOptions();
                    var options = LoggerExtensions.Options;

                    if (options.AllowFilterDynamicOverride)
                    {
                        config.ReadFrom.Configuration(host.Configuration).MinimumLevel.ControlledBy(LogFilterOverrideScope.EnableFilterOverride(options.MinimumLevel)).Enrich.FromLogContext();
                    }
                    else
                    {
                        config.ReadFrom.Configuration(host.Configuration).Enrich.FromLogContext();
                    }

                    if (options.AllowDestructureUsingAttributes)
                    {
                        config = config.Destructure.UsingAttributes();
                    }

                    if (instanceMeta != null)
                    {
                        config = config
                        .Enrich.WithProperty("AppVer", meta.Version)
                        .Enrich.WithProperty(nameof(vmRegion), vmRegion)
                        .Enrich.WithProperty("vmName", instanceMeta.Compute.Name)
                        .Enrich.WithProperty("vmRG", instanceMeta.Compute.ResourceGroupName);

                        var objectId = meta?.GetComputeTagMetadata()?.LiftrObjectId;
                        if (!string.IsNullOrEmpty(objectId))
                        {
                            config = config
                            .Enrich.WithProperty("LiftrObjectId", objectId);
                        }
                    }

                    if (!host.Configuration.ContainsSerilogWriteToConsole())
                    {
                        config = config.WriteTo.Console(new CompactJsonFormatter(renderMessage: options.RenderMessage));
                    }
                })
                .ConfigureServices((host, services) =>
                {
                    services.AddSingleton<ITelemetryInitializer>(liftrTelemetryInitializer);

                    LoggerExtensions.Options = host.Configuration.ExtractLoggingOptions();
                    var options = LoggerExtensions.Options;

                    if (!string.IsNullOrEmpty(options.AppInsigthsInstrumentationKey))
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        var builder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;
#pragma warning restore CS0618 // Type or member is obsolete
                        builder.Use((next) => new LiftrTelemetryProcessor(next));
                        builder.Build();
                        services.AddApplicationInsightsTelemetry();
                    }

                    services.AddSingleton(new HttpCoreDiagnosticSourceSubscriber(new HttpCoreDiagnosticSourceListener()));

                    services.AddSingleton<IStartupFilter>((sp) =>
                    {
                        return new LoggingMiddlewareStartupFilter(options.LogRequest, options.LogSubdomain, options.LogHostName);
                    });

                    services.AddSingleton<Serilog.ILogger>((sp) =>
                    {
                        if (!string.IsNullOrEmpty(options.AppInsigthsInstrumentationKey))
                        {
                            var appInsightsClient = sp.GetService<TelemetryClient>();
                            AppInsightsHelper.AppInsightsClient = appInsightsClient;
                        }

                        return Log.Logger;
                    });
                });

            return webHostBuilder;
        }
    }
}
