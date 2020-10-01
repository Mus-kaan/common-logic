//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Liftr.Logging
{
    public class LocalHostTelemetryFilter : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public LocalHostTelemetryFilter(ITelemetryProcessor next)
        {
            _next = next;
        }

        public void Process(ITelemetry item)
        {
            var dependencyTelemetry = item as DependencyTelemetry;
            if (dependencyTelemetry != null &&
                dependencyTelemetry.Data?.OrdinalContains("localhost") == true)
            {
                return;
            }
            else
            {
                _next.Process(item);
            }
        }
    }
}
