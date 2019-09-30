//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.GenericHosting")]

namespace Microsoft.Liftr.Logging
{
    internal static class AppInsightsHelper
    {
        public static TelemetryClient AppInsightsClient { get; set; }
    }
}
