﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Liftr.Logging
{
    public class NoSamplingTelemetryProcessor : ITelemetryProcessor
    {
        public void Process(ITelemetry item)
        {
            if (item is ISupportSampling)
            {
                ((ISupportSampling)item).SamplingPercentage = 100;
            }
        }
    }
}
