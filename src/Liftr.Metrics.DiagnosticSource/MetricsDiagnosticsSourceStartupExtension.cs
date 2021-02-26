//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Liftr.Logging.Metrics;
using Serilog;
using System;

namespace Microsoft.Liftr.Metrics.DiagnosticSource
{
    /// <summary>
    /// Metric diagnostic startup extension
    /// </summary>
    public static class MetricsDiagnosticsSourceStartupExtension
    {
        public static void AddMetricDiagnosticService(this IServiceCollection services, IMetricSender metricSender, ILogger logger)
        {
            if (metricSender == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var listener = new MetricsDiagnosticSourceListener(metricSender, logger);
#pragma warning disable CA2000 // Dispose objects before losing scope
            services.AddSingleton(new MetricsDiagnosticSourceSubscriber(listener));
#pragma warning restore CA2000 // Dispose objects before losing scope
        }
    }
}
