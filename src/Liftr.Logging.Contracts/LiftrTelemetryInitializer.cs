//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Liftr.Logging.Contracts
{
    public class LiftrTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _vmRegion;

        public LiftrTelemetryInitializer(string vmRegion)
        {
            _vmRegion = vmRegion;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (_vmRegion != null && (telemetry is RequestTelemetry || telemetry is DependencyTelemetry))
            {
                var propertyItem = telemetry as ISupportProperties;
                if (propertyItem != null && propertyItem.Properties != null)
                {
                    propertyItem.Properties["vmRegion"] = _vmRegion;
                }
            }
        }
    }
}
