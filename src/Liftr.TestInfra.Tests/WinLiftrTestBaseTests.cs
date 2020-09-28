//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Tests;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.TestInfra.Tests
{
    public class WinLiftrTestBaseTests : LiftrTestBase
    {
        public WinLiftrTestBaseTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void SupportNet471()
        {
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
            Task.Delay(2000).Wait();
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
        }
    }
}
