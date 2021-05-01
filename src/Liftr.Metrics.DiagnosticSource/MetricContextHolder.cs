//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading;

namespace Microsoft.Liftr.Metrics.DiagnosticSource
{
    public static class MetricContextHolder
    {
        /// <summary>
        /// If this hold value, then MetaRP metrics are sent.
        /// </summary>
        public static readonly AsyncLocal<string> MetaRPMetricContext = new AsyncLocal<string>();

        /// <summary>
        /// If this hold value, then Marketplace metrics are sent.
        /// </summary>
        public static readonly AsyncLocal<string> MarketplaceMetricContext = new AsyncLocal<string>();

        /// <summary>
        /// If this hold value, then Partner metrics are sent.
        /// </summary>
        public static readonly AsyncLocal<string> PartnerMetricContext = new AsyncLocal<string>();
    }
}
