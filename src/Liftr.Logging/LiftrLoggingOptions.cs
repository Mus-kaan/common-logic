//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog.Events;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Configuration")]

namespace Microsoft.Liftr.Logging
{
    public sealed class LiftrLoggingOptions
    {
        public bool RenderMessage { get; internal set; } = false;

        public bool LogTimedOperation { get; internal set; } = true;

        public bool LogRequest { get; internal set; } = true;

        public bool AllowFilterDynamicOverride { get; internal set; } = false;

        public bool AllowDestructureUsingAttributes { get; internal set; } = false;

        public LogEventLevel MinimumLevel { get; internal set; } = LogEventLevel.Information;

        public string AppInsigthsInstrumentationKey { get; internal set; } = null;

        public bool WriteToConsole { get; internal set; } = false;

        public bool LogHostName { get; internal set; } = false;

        public bool LogSubdomain { get; internal set; } = false;
    }
}
