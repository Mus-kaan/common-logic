//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.Contracts.Tests
{
    public class AzureStorageConnectionStringTests
    {
        private static readonly string s_conStr = "RGVmYXVsdEVuZHBvaW50c1Byb3RvY29sPWh0dHBzO0FjY291bnROYW1lPWV4cG9ydDk4NjgzNTU3ZGI3NTk7QWNjb3VudEtleT1JTlZBTElES2dUVExDTlF2cERpQ011N3dFUjI5VkZOTkgxVXZieng3NDJOanRRZlNUZEZhMmVrOGp3V09EbGw4UWY1MXFxRFFRTGF5KzNCNkVRdDFnPT07RW5kcG9pbnRTdWZmaXg9Y29yZS53aW5kb3dzLm5ldA==".FromBase64();

        [Fact]
        public void VerifyEnvironmentTypeShortNames()
        {
            AzureStorageConnectionString str = null;
            Assert.True(AzureStorageConnectionString.TryParseConnectionString(s_conStr, out str));
            Assert.Equal(s_conStr, str.ToString());
        }
    }
}
