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
        [SkipInOfficialBuild(skipLinux: true)]
        public async Task CanGetTenantIdAsync()
        {
            using var helper = new TenantHelper(new Uri("https://management.azure.com"));
            var tenantId = await helper.GetTenantIdForSubscriptionAsync("8f59a6fe-696c-45e1-8a91-d2ccb55871fc");
            Assert.Equal("72f988bf-86f1-41af-91ab-2d7cd011db47", tenantId);
        }
    }
}
