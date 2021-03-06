//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Metrics
{
    /// <summary>
    /// Method Interception Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LiftrMetricsAttribute : Attribute
    {
        public LiftrMetricsAttribute(string metricName)
        {
            MetricName = metricName ?? throw new ArgumentNullException(nameof(metricName));
        }

        public string MetricName { get; }
    }
}
