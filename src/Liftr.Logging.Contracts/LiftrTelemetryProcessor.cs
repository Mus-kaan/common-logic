//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Liftr.Logging
{
    public class LiftrTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public LiftrTelemetryProcessor(ITelemetryProcessor next)
        {
            _next = next;
        }

        public void Process(ITelemetry item)
        {
            // Skip sending data to AppInsights in scope of 'NoAppInsightsScope'.
            if (AppInsightsHelper.SkipAppInsightsCount.Value > 0 &&
                !(item is ExceptionTelemetry))
            {
                return;
            }

            if (item is ISupportSampling)
            {
                // Disable AppInsights telemetry data sampling.
                ((ISupportSampling)item).SamplingPercentage = 100;
            }

            _next.Process(item);
        }
    }
}
