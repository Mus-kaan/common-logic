//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.TokenManager.Tests
{
    public class TenantHelperTests
    {
        [SkipInOfficialBuild(skipLinux: true)]
        public async Task CanGetTenantIdAsync()
        {
            using var tenantHelper = new TenantHelper(new Uri("https://management.azure.com"));
            var tenantId = await tenantHelper.GetTenantIdForSubscriptionAsync("eebfbfdb-4167-49f6-be43-466a6709609f");
            Assert.Equal("72f988bf-86f1-41af-91ab-2d7cd011db47", tenantId);
        }
    }
}
