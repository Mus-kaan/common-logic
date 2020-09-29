//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Logging
{
    /// <summary>
    /// Skip sending Telemetry data to AppInsights in this scope (except exceptions).
    /// </summary>
    public sealed class NoAppInsightsScope : IDisposable
    {
        private readonly bool _skip;

        public NoAppInsightsScope(bool skip = true)
        {
            _skip = skip;
            if (_skip)
            {
                AppInsightsHelper.SkipAppInsightsCount.Value++;
            }
        }

        public void Dispose()
        {
            if (_skip)
            {
                AppInsightsHelper.SkipAppInsightsCount.Value--;
            }
        }
    }
}
