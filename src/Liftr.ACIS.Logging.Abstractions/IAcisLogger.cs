//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Logging;
using System;

namespace Microsoft.Liftr.ACIS.Logging
{
    public interface IAcisLogger
    {
        Serilog.ILogger Logger { get; }

        ITimedOperation StartTimedOperation(
            string operationName,
            string operationId = null,
            bool generateMetrics = false,
            bool newCorrelationId = false,
            bool skipAppInsights = false);

        void LogError(string message);

        void LogError(Exception exception, string message);

        void LogInfo(string message);

        void LogVerbose(string message);

        void LogWarning(string message);
    }
}
