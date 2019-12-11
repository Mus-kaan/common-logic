//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using System;

namespace Microsoft.Liftr.EV2
{
    public static class SimpleCustomLogSink
    {
        private static TelemetryConfiguration s_appInsightsConfig;
        private static TelemetryClient s_appInsightsClient;

        public static Serilog.ILogger GetLogger()
        {
            // /subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourcegroups/liftr-dev-wus-rg/providers/microsoft.insights/components/liftr-ev2-artifacts-generator
            s_appInsightsConfig = s_appInsightsConfig ?? new TelemetryConfiguration("fd294aff-6c0d-4b01-8505-2edd08005269");
            s_appInsightsClient = s_appInsightsClient ?? new TelemetryClient(s_appInsightsConfig);

            var loggerConfig = new LoggerConfiguration()
                .Enrich.WithProperty("BuildSessionId", Guid.NewGuid().ToString())
                .Enrich.WithProperty("BuildStartTime", DateTime.UtcNow.ToZuluString())
                .Enrich.WithProperty("BuildMachineName", Environment.MachineName)
                .WriteTo.Console()
                .WriteTo.ApplicationInsights(s_appInsightsClient, TelemetryConverter.Events);

            return loggerConfig.Enrich.FromLogContext().CreateLogger();
        }

        public static void Flush()
        {
            s_appInsightsClient?.Flush();
            s_appInsightsConfig?.Dispose();
        }
    }
}
