//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Tests;
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
            Logger.Information("TestLogEvent");
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
            Task.Delay(2000).Wait();
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
        }
    }
}
