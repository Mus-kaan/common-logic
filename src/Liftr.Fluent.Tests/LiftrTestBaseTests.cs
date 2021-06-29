//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr.Logging.StaticLogger;
using Microsoft.Liftr.Tests;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class LiftrTestBaseTests : LiftrTestBase
    {
        public LiftrTestBaseTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void SimpleTest()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                StaticLiftrLogger.Logger.Information("test");
            });

            StaticLiftrLogger.Initilize("78b3bb82-b6b7-42bf-93d8-c8ba1ca26331");
            StaticLiftrLogger.Logger.Information("test");

            Logger.Information("TestLogEvent");

            var logs = GetLogEvents();

            logs.Last().MessageTemplate.Should().Equals("TestLogEvent");

#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
            Task.Delay(2000).Wait();
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
        }
    }
}
