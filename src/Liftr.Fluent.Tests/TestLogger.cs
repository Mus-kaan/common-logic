//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public static class TestLogger
    {
        public static ILogger GetLogger(ITestOutputHelper output) => new LoggerConfiguration().WriteTo.Xunit(output).CreateLogger();
    }
}
