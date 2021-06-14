//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class AcrTests : LiftrAzureTestBase
    {
        public AcrTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicEastUS]
        public async Task CreateAcrAsync()
        {
            var az = Client;
            var name = SdkContext.RandomResourceName("acr", 15);
            var acr = await az.GetOrCreateACRAsync(Location, ResourceGroupName, name, Tags);
            Assert.Equal("Premium", acr.Sku.Name);
        }
    }
}
