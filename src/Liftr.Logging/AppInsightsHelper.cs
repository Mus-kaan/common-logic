//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.GenericHosting")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Tests.Common")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Tests.Utilities")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.StaticLogger")]

namespace Microsoft.Liftr.Logging
{
    internal static class AppInsightsHelper
    {
        /// <summary>
        /// Count of skip AppInsight requests.
        /// </summary>
        public static readonly AsyncLocal<int> SkipAppInsightsCount = new AsyncLocal<int>();

        public static TelemetryClient AppInsightsClient { get; set; }
    }
}
