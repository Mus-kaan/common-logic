//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Core;
using System;
using System.Globalization;

namespace Microsoft.Liftr.Logging.StaticLogger
{
    public static class StaticLiftrLogger
    {
        private static TelemetryConfiguration s_appInsightsConfig;
        private static DependencyTrackingTelemetryModule s_depModule;
        private static TelemetryClient s_appInsightsClient;
        private static Logger s_logger;

        public static ILogger Logger
        {
            get
            {
                if (s_logger == null)
                {
                    throw new InvalidOperationException($"Please call {nameof(StaticLiftrLogger)}.{nameof(Initilize)} first.");
                }

                return s_logger;
            }
        }

        public static void Initilize(string appInsightsInstrumentationKey)
        {
            if (s_appInsightsConfig != null)
            {
                throw new InvalidOperationException($"{nameof(StaticLiftrLogger)}.{nameof(Initilize)} cannot be called multiple times.");
            }

            s_appInsightsConfig = new TelemetryConfiguration(appInsightsInstrumentationKey);
            var builder = s_appInsightsConfig.TelemetryProcessorChainBuilder;
            builder.Use((next) => new LiftrTelemetryProcessor(next));
            builder.Build();

            s_depModule = new DependencyTrackingTelemetryModule();
            s_depModule.Initialize(s_appInsightsConfig);
            s_appInsightsClient = new TelemetryClient(s_appInsightsConfig);

            var loggerConfig = new LoggerConfiguration()
                .Enrich.WithProperty("ProcessSessionId", Guid.NewGuid().ToString())
                .Enrich.WithProperty("ProcessStartTime", DateTime.UtcNow.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture))
                .WriteTo.ApplicationInsights(s_appInsightsClient, TelemetryConverter.Traces);

            s_logger = loggerConfig.Enrich.FromLogContext().CreateLogger();
        }
    }
}
