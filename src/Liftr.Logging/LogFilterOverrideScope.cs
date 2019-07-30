//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog.Core;
using Serilog.Events;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.AspNetCore")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.AspNetCore.Tests")]

namespace Microsoft.Liftr.Logging
{
    /// <summary>
    /// Define a scope to override the log level filter.
    /// Normally, the minimum log filter (e.g. Information) is defined in the appsettings.json.
    /// For performance consideration, only the logs above this minimum level is emitted, i.e. the log content is serialized to json and wrote to configured sinks (e.g. console, file or AppInsights).
    /// For some scenarios, we might want to lower the minimum level filter to emit more logs.
    /// During the scope of an instance of this class, the log level will be override to be not higher than the overriding level specified in the constructor.
    /// </summary>
    public sealed class LogFilterOverrideScope : IDisposable
    {
        // The global logging level switch configured for Serilog Logger.
        // For more details: https://nblumhardt.com/2014/10/dynamically-changing-the-serilog-level/
        private static readonly LoggingLevelSwitch s_globalSwitch = new LoggingLevelSwitch();
        private static LogEventLevel? s_defaultLevel = null;

        // The number of current effective 'LogFilterOverrideScope' instance.
        // As long as the this number is bigger than 0, the lowest filter level seen will be used.
        // When the number changed to 0, the default filter level will be brought back.
        // e.g. Default filter is 'Error' accorind to appsettings.json.  Only 'Error' and 'Fatal' will be emitted.
        // Request 1 statred with overriding filter of 'Information'. s_overrideCount = 1. Now, 'Information', 'Warning', 'Error' and 'Fatal' will be emitted.
        // Request 2 statred with overriding filter of 'Warning'. s_overrideCount = 2. Since Request 1's 'Information' is lower than 'Warning'. 'Information' will be the minimum level.
        // Request 1 stopped. s_overrideCount = 1. Filter is still 'Information' although R1 ended, R2's level is 'Warning' which is higher than 'Information'.
        // R2 stopped. s_overrideCount = 0. The minimum back to the default 'Error'.
        // Although this simple counter implementation may let the filter be lower than the actual requested level,
        // this is simpler and faster than the prioty-queue-like implementation.
        private static int s_overrideCount = 0;
        private static object s_syncObj = new object();

        private readonly LogEventLevel? _filterOverride = null;

        public LogFilterOverrideScope(LogEventLevel? filterOverride = null)
        {
            if (!filterOverride.HasValue || !s_defaultLevel.HasValue)
            {
                return;
            }

            _filterOverride = filterOverride;
            lock (s_syncObj)
            {
                s_overrideCount++;
                if (filterOverride < s_globalSwitch.MinimumLevel)
                {
                    s_globalSwitch.MinimumLevel = filterOverride.Value;
                }
            }
        }

        public void Dispose()
        {
            if (!_filterOverride.HasValue)
            {
                return;
            }

            lock (s_syncObj)
            {
                s_overrideCount--;
                if (s_overrideCount <= 0)
                {
                    s_globalSwitch.MinimumLevel = s_defaultLevel.Value;
                    s_overrideCount = 0;
                }
            }
        }

        internal static LoggingLevelSwitch EnableFilterOverride(LogEventLevel defaultLevel)
        {
            s_defaultLevel = defaultLevel;
            s_globalSwitch.MinimumLevel = defaultLevel;
            return s_globalSwitch;
        }
    }
}
