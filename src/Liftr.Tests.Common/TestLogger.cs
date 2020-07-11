//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog;
using System;
using Xunit.Abstractions;

namespace Microsoft.Liftr
{
    public static class TestLogger
    {
        public static ILogger VoidLogger => new LoggerConfiguration().CreateLogger();

        public static ILogger GenerateLogger(ITestOutputHelper output)
        {
            var loggerConfig = new LoggerConfiguration()
            .Enrich.WithProperty("UnitTestessionId", Guid.NewGuid().ToString())
                .Enrich.WithProperty("UnitTestStartTime", DateTime.UtcNow.ToZuluString());

            if (output != null)
            {
                loggerConfig = loggerConfig.WriteTo.Xunit(output);
            }

            return loggerConfig.Enrich.FromLogContext().CreateLogger();
        }
    }
}
