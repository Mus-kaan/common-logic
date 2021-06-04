//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Liftr.Logging.Contracts;
using Microsoft.Liftr.Utilities;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Metrics.Prom
{
    public static class WebHostBuilderExtension
    {
        /// <summary>
        /// Expose the duration metrics of <see cref="LoggerExtensions.StartTimedOperation" /> to Prometheus
        /// </summary>
        public static IWebHostBuilder UsePrometheusMetrics(this IWebHostBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (!PrometheusMetricsProcessor.Enabled)
            {
                var meta = InstanceMetaHelper.GetMetaInfoAsync().Result;
                var instanceMeta = meta.InstanceMeta;

                var staticLabels = new Dictionary<string, string>();

                if (meta != null)
                {
                    staticLabels["Assembly"] = meta.AssemblyName;
                    staticLabels["Version"] = meta.Version;

                    if (instanceMeta?.Compute != null)
                    {
                        staticLabels["vmLocation"] = instanceMeta.Compute.Location;
                        staticLabels["vmName"] = instanceMeta.Compute.Name;
                    }

                    Prometheus.Metrics.DefaultRegistry.SetStaticLabels(staticLabels);
                }

                PrometheusMetricsProcessor.TimedOperationMetricsProcessor = new TimedOperationPrometheusProcessor();
                PrometheusMetricsProcessor.Enabled = true;
            }

            return builder;
        }
    }
}