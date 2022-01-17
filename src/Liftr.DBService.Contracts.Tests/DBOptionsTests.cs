//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.DBService.Contracts.Tests
{
    public class DBOptionsTests
    {
        [Fact]
        public void Validate()
        {
            var dbOptions = new DBOptions() { DatabaseName = "test", LogDBOperation = true, PrimaryConnectionString = "cnstr", SecretKey = "na" };
            Assert.True(dbOptions.Validate() == true);
        }

        [Fact]
        public void LogDBOperationShouldBeTrue()
        {
            var dbOptions = new DBOptions() { DatabaseName = "test", PrimaryConnectionString = "cnstr", SecretKey = "na" };
            Assert.True(dbOptions.LogDBOperation == true);
        }
    }
}