//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Liftr.Logging.Metrics;
using Microsoft.Liftr.Utilities;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Metrics.AspNetCore
{
    public static class LiftrMetricsStartupExtensions
    {
        /// <summary>
        /// Add this to IServiceCollection to send the app metrics to geneva
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="serviceName"></param>
        /// <param name="logger"></param>
        public static void AddMetricSenderService(this IServiceCollection services, IConfiguration configuration, string serviceName, Serilog.ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            services.AddSingleton<IMetricSender>((sp) =>
            {
                var meta = InstanceMetaHelper.GetMetaInfoAsync().GetAwaiter().GetResult();
                var instanceMeta = meta.InstanceMeta;
                Dictionary<string, string> defaultDimensions = null;
                if (instanceMeta != null && instanceMeta.Compute != null)
                {
                    defaultDimensions = new Dictionary<string, string>
                    {
                        ["Region"] = instanceMeta.Compute.Location,
                        ["VmName"] = instanceMeta.Compute.Name,
                        ["ServiceName"] = serviceName,
                    };
                }

                if (defaultDimensions != null)
                {
                    logger.Information("info_metricsender_dimensions. defaultDimensions: {@defaultDimensions}", defaultDimensions);
                }
                else
                {
                    logger.Information("info_metricsender_dimensions: No dimensions");
                }

                logger.Information("info_metricsender_create: StatsDHost: {statsDHost}, nameSpace: {namespace}", "localhost", "AppMetrics");

                logger.Information("info_metricsender_create: StatsDHost: {statsDHost}, nameSpace: {namespace}", "localhost", "AppMetrics");

                var mdmHost = configuration["MdmHost"];

                if (mdmHost == null)
                {
                    var ex = new InvalidOperationException($"[{nameof(AddMetricSenderService)}] Please make sure mdmhost is set in configuration");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                return new MetricSender(mdmHost, "AppMetrics", logger, defaultDimensions);
            });
        }
    }
}
