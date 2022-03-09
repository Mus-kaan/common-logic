//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Logging.Contracts;
using Microsoft.Liftr.Utilities;
using Prometheus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Liftr.Metrics.Prom
{
    public class TimedOperationPrometheusProcessor : ITimedOperationMetricsProcessor
    {
        private const string c_asyncSuffic = "Async";
        private readonly ConcurrentDictionary<string, Summary> _summaries = new ConcurrentDictionary<string, Summary>();

        public void Process(ITimedOperation timedOperation)
        {
            if (timedOperation == null)
            {
                throw new ArgumentNullException(nameof(timedOperation));
            }

            try
            {
                var cacheKey = timedOperation.CallerFilePath + timedOperation.Name;
                var summary = _summaries.GetOrAdd(cacheKey, (_) =>
                {
                    var name = timedOperation.Name;

                    if (name.OrdinalEndsWith(c_asyncSuffic))
                    {
                        name = name.Substring(0, name.Length - c_asyncSuffic.Length);
                    }

                    var summaryName = PrometheusHelper.ConvertToPrometheusMetricsName("app_" + name + "DurationMilliseconds");

                    var labelNames = new List<string> { "result" };

                    if (timedOperation.MetricLabels?.Any() == true)
                    {
                        labelNames.AddRange(timedOperation.MetricLabels.Select(kvp => kvp.Key));
                    }

                    var summaryConfig = new SummaryConfiguration
                    {
                        Objectives = new[]
                        {
                        new QuantileEpsilonPair(0.5, 0.05),
                        new QuantileEpsilonPair(0.75, 0.05),
                        new QuantileEpsilonPair(0.95, 0.01),
                        new QuantileEpsilonPair(0.99, 0.005),
                        },
                        LabelNames = labelNames.ToArray(),
                    };
                    var newSummary = Prometheus.Metrics
                        .CreateSummary(summaryName, $"A summary of duration of the operation '{timedOperation.Name}' at [{Path.GetFileName(timedOperation.CallerFilePath)}:{timedOperation.CallerMemberName}:{timedOperation.CallerLineNumber}]", summaryConfig);
                    return newSummary;
                });

                var labelValues = new List<string> { timedOperation.IsSuccessful ? "success" : "failure" };

                if (timedOperation.MetricLabels?.Any() == true)
                {
                    labelValues.AddRange(timedOperation.MetricLabels.Select(kvp => kvp.Value));
                }

                try
                {
                    summary
                        .WithLabels(labelValues.ToArray())
                        .Observe(timedOperation.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, $"err_emit_prom_metrics. timedOperationName: {timedOperation.Name}, labelNames: ({string.Join(", ", summary.LabelNames)}), labelValues: ({string.Join(", ", labelValues)})");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, $"err_emit_prom_metrics. Failed at generating Prometheus metrics. timedOperationName: {timedOperation.Name}");
            }
        }
    }
}
