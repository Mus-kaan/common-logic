//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog.Context;
using System;

namespace Microsoft.Liftr.Logging
{
    public sealed class ARMHeaderLogContext : IDisposable
    {
        private readonly IDisposable _trackingIdContext;
        private readonly IDisposable _correlationIdContext;

        public ARMHeaderLogContext(string trackingId, string correalationId)
        {
            _trackingIdContext = string.IsNullOrEmpty(trackingId) ? null : LogContext.PushProperty("ARMTrackingId", trackingId);
            _correlationIdContext = string.IsNullOrEmpty(correalationId) ? null : LogContext.PushProperty("ARMCorrelationId", correalationId);
        }

        public void Dispose()
        {
            _trackingIdContext?.Dispose();
            _correlationIdContext?.Dispose();
        }
    }
}
