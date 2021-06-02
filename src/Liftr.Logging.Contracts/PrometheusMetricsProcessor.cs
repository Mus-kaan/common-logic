//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Metrics.Prom")]

namespace Microsoft.Liftr.Logging.Contracts
{
    internal class PrometheusMetricsProcessor
    {
        public static bool Enabled { get; set; }

        public static ITimedOperationMetricsProcessor TimedOperationMetricsProcessor { get; set; }
    }
}
