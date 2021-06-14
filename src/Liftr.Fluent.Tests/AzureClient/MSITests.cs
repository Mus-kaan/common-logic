//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class MSITests : LiftrAzureTestBase
    {
        public MSITests(ITestOutputHelper output)
            : base(output)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS2]
        public async Task CanCreateAsync()
        {
            var client = Client;
            var name = SdkContext.RandomResourceName("test-msi-", 15);

            var created = await client.CreateMSIAsync(Location, ResourceGroupName, name, Tags);
            var retrieved = await client.GetMSIAsync(ResourceGroupName, name);

            Assert.Equal(name, retrieved.Name);
            TestCommon.CheckCommonTags(retrieved.Inner.Tags);
        }
    }
}
