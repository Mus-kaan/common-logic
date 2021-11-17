//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Utilities.Tests
{
    public class TenantHelperTest
    {
        [CheckInValidation(skipLinux: true)]
        public async Task CanGetTenantIdAsync()
        {
            using var helper = new TenantHelper(new Uri("https://management.azure.com"));
            var tenantId = await helper.GetTenantIdForSubscriptionAsync("f885cf14-b751-43c1-9536-dc5b1be02bc0"); // subscription 'LiftrToolsUnitTestONLY'
            Assert.Equal("72f988bf-86f1-41af-91ab-2d7cd011db47", tenantId);
        }
    }
}
