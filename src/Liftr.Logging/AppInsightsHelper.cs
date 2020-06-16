//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.GenericHosting")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Tests.Common")]

namespace Microsoft.Liftr.Logging
{
    internal static class AppInsightsHelper
    {
        public static TelemetryClient AppInsightsClient { get; set; }
    }
}
