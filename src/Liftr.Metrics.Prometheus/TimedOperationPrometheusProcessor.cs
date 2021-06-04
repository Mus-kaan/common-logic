//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Logging.Contracts;
using Prometheus;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;

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

            var cacheKey = timedOperation.CallerFilePath + timedOperation.Name;
            var summary = _summaries.GetOrAdd(cacheKey, (_) =>
            {
                var name = timedOperation.Name;

                if (name.OrdinalEndsWith(c_asyncSuffic))
                {
                    name = name.Substring(0, name.Length - c_asyncSuffic.Length);
                }

                var summaryName = ConvertToPrometheusMetricsName("app_" + name + "DurationMilliseconds");
                var summaryConfig = new SummaryConfiguration
                {
                    Objectives = new[]
                    {
                        new QuantileEpsilonPair(0.5, 0.05),
                        new QuantileEpsilonPair(0.75, 0.05),
                        new QuantileEpsilonPair(0.95, 0.01),
                        new QuantileEpsilonPair(0.99, 0.005),
                    },
                    LabelNames = new[] { "result" },
                };
                var newSummary = Prometheus.Metrics
                    .CreateSummary(summaryName, $"A summary of duration of the operation '{timedOperation.Name}' at [{Path.GetFileName(timedOperation.CallerFilePath)}:{timedOperation.CallerMemberName}:{timedOperation.CallerLineNumber}]", summaryConfig);
                return newSummary;
            });

            summary
                .WithLabels(timedOperation.IsSuccessful ? "success" : "failure")
                .Observe(timedOperation.ElapsedMilliseconds);
        }

        public static string ConvertToPrometheusMetricsName(string name)
        {
            // Prometheus metrics name format: '^[a-zA-Z_:][a-zA-Z0-9_:]*$'.)'
            // https://github.com/efcore/EFCore.NamingConventions/blob/main/EFCore.NamingConventions/Internal/SnakeCaseNameRewriter.cs
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var builder = new StringBuilder(name.Length + Math.Min(2, name.Length / 5));
            var previousCategory = default(UnicodeCategory?);

            for (var currentIndex = 0; currentIndex < name.Length; currentIndex++)
            {
                var currentChar = name[currentIndex];
                if (currentChar == '_')
                {
                    builder.Append('_');
                    previousCategory = null;
                    continue;
                }

                var currentCategory = char.GetUnicodeCategory(currentChar);
                switch (currentCategory)
                {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                        if (previousCategory == UnicodeCategory.SpaceSeparator ||
                            previousCategory == UnicodeCategory.LowercaseLetter ||
                            (previousCategory != UnicodeCategory.DecimalDigitNumber &&
                            previousCategory != null &&
                            currentIndex > 0 &&
                            currentIndex + 1 < name.Length &&
                            char.IsLower(name[currentIndex + 1])))
                        {
                            builder.Append('_');
                        }

                        currentChar = char.ToLower(currentChar, CultureInfo.InvariantCulture);
                        break;

                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.DecimalDigitNumber:
                        if (previousCategory == UnicodeCategory.SpaceSeparator)
                        {
                            builder.Append('_');
                        }

                        break;

                    default:
                        if (previousCategory != null)
                        {
                            previousCategory = UnicodeCategory.SpaceSeparator;
                        }

                        continue;
                }

                builder.Append(currentChar);
                previousCategory = currentCategory;
            }

            return builder.ToString();
        }
    }
}
