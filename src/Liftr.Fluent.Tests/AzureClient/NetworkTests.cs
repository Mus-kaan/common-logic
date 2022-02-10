//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class NetworkTests : LiftrAzureTestBase
    {
        public NetworkTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS2]
        public async Task CanCreateTrafficManagerAsync()
        {
            using var tmLock = await AcquireTrafficManaderTestLockAsync();

            var client = Client;
            var tmName = SdkContext.RandomResourceName("test-tm", 15);
            var pipName = SdkContext.RandomResourceName("pip", 9);
            var vnetName = SdkContext.RandomResourceName("vnet", 9);
            var created = await client.CreateTrafficManagerAsync(ResourceGroupName, tmName, TestCommon.Tags);

            // Second deployment will not fail.
            var tm2 = await client.CreateTrafficManagerAsync(ResourceGroupName, tmName, TestCommon.Tags);

            var retrieved = await client.GetTrafficManagerAsync(created.Id);
            Assert.NotNull(retrieved);

            Assert.Equal(tmName, retrieved.Name);
            TestCommon.CheckCommonTags(retrieved.Inner.Tags);

            var helper = new AKSNetworkHelper(Logger);

            await helper.AddPulicIpToTrafficManagerAsync(Client.FluentClient, created.Id, "endpoint1", "40.76.4.151", enabled: false);
            await helper.AddPulicIpToTrafficManagerAsync(Client.FluentClient, created.Id, "endpoint1", "40.76.4.151", enabled: true);

            await helper.AddPulicIpToTrafficManagerAsync(Client.FluentClient, created.Id, "endpoint2", "40.76.4.150", enabled: true);
            await helper.AddPulicIpToTrafficManagerAsync(Client.FluentClient, created.Id, "endpoint2", "40.76.4.150", enabled: false);

            var vnet = await client.GetOrCreateVNetAsync(TestCommon.Location, ResourceGroupName, vnetName, TestCommon.Tags);
            Assert.NotNull(vnet);
            Assert.NotNull(await client.GetOrCreateVNetAsync(TestCommon.Location, ResourceGroupName, vnetName, TestCommon.Tags));

            Assert.NotNull(await client.GetOrCreatePublicIPAsync(TestCommon.Location, ResourceGroupName, pipName, TestCommon.Tags));
            Assert.NotNull(await client.GetOrCreatePublicIPAsync(TestCommon.Location, ResourceGroupName, pipName, TestCommon.Tags));

            var subnet1 = await client.CreateNewSubnetAsync(vnet, "subnet1");
            Assert.Equal("10.66.1.0/24", subnet1.AddressPrefix);

            var subnet2 = await client.CreateNewSubnetAsync(vnet, "subnet2");
            Assert.Equal("10.66.2.0/24", subnet2.AddressPrefix);

            var subnet3 = await client.CreateNewSubnetAsync(vnet, "subnet3");
            Assert.Equal("10.66.3.0/24", subnet3.AddressPrefix);
        }
    }
}
