﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog;

namespace Microsoft.Liftr.Logging
{
    public static class LoggerFactory
    {
        public static ILogger VoidLogger => new LoggerConfiguration().CreateLogger();
    }
}
