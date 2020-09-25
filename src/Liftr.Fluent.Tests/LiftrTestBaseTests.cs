//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Tests;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class LiftrTestBaseTests : LiftrTestBase
    {
        [Fact]
        public void SimpleTest()
        {
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
            Task.Delay(2000).Wait();
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
        }
    }
}
