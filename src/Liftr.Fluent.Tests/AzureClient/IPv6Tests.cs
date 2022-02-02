//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class IPv6Tests : LiftrAzureTestBase
    {
        public IPv6Tests(ITestOutputHelper output)
            : base(output)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS2]
        public async Task CanCreatePublicIPv6Async()
        {
            var client = Client;
            var pipName = SdkContext.RandomResourceName("pip", 9);

            await client.GetOrCreatePublicIPv6Async(TestCommon.Location, ResourceGroupName, pipName, TestCommon.Tags);

            // Second deployment will not fail.
            var pip = await client.GetOrCreatePublicIPv6Async(TestCommon.Location, ResourceGroupName, pipName, TestCommon.Tags);

            Assert.NotNull(pip);
            Assert.Equal(IPVersion.IPv6, pip.Version);
        }
    }
}
