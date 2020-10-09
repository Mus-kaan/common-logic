//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.ACIS.Logging;
using Microsoft.Liftr.Logging;
using Serilog;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Tests.Common
{
    public class MockAcisLogger : IAcisLogger
    {
        public MockAcisLogger(ILogger logger)
        {
            Logger = logger;
        }

        public List<string> Logs { get; } = new List<string>();

        public List<string> InfoLogs { get; } = new List<string>();

        public List<string> ErrorLogs { get; } = new List<string>();

        public ILogger Logger { get; }

        public void LogError(string message)
        {
            Logger.Error(message);
            Logs.Add(message);
            ErrorLogs.Add(message);
        }

        public void LogError(Exception exception, string message)
        {
            Logger.Error(exception, message);
            Logs.Add(message);
            ErrorLogs.Add(message);
        }

        public void LogInfo(string message)
        {
            Logger.Information(message);
            Logs.Add(message);
            InfoLogs.Add(message);
        }

        public void LogVerbose(string message)
        {
            Logger.Verbose(message);
            Logs.Add(message);
        }

        public void LogWarning(string message)
        {
            Logger.Warning(message);
            Logs.Add(message);
        }

        public ITimedOperation StartTimedOperation(string operationName, string operationId = null, bool generateMetrics = false, bool newCorrelationId = false, bool skipAppInsights = false)
        {
            return Logger.StartTimedOperation(operationName, operationId, generateMetrics, newCorrelationId, skipAppInsights);
        }
    }
}
