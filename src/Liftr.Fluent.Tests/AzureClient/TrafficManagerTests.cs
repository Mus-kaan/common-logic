//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Provisioning;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class TrafficManagerTests
    {
        private readonly ITestOutputHelper _output;

        public TrafficManagerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanCreateTrafficManagerAsync()
        {
            // This test will normally take about 12 minutes.
            using (var scope = new TestResourceGroupScope("unittest-trafficmanager-", _output))
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var tmName = SdkContext.RandomResourceName("test-tm", 15);
                var pipName = SdkContext.RandomResourceName("pip", 9);
                var created = await client.CreateTrafficManagerAsync(scope.ResourceGroupName, tmName, TestCommon.Tags);

                // Second deployment will not fail.
                var tm2 = await client.CreateTrafficManagerAsync(scope.ResourceGroupName, tmName, TestCommon.Tags);

                var retrieved = await client.GetTrafficManagerAsync(created.Id);
                Assert.NotNull(retrieved);

                Assert.Equal(tmName, retrieved.Name);
                TestCommon.CheckCommonTags(retrieved.Inner.Tags);

                var helper = new AKSHelper(scope.Logger);

                await helper.AddPulicIpToTrafficManagerAsync(scope.Client.FluentClient, created.Id, "endpoint1", "40.76.4.151", enabled: false);
                await helper.AddPulicIpToTrafficManagerAsync(scope.Client.FluentClient, created.Id, "endpoint1", "40.76.4.151", enabled: true);

                await helper.AddPulicIpToTrafficManagerAsync(scope.Client.FluentClient, created.Id, "endpoint2", "40.76.4.150", enabled: true);
                await helper.AddPulicIpToTrafficManagerAsync(scope.Client.FluentClient, created.Id, "endpoint2", "40.76.4.150", enabled: false);
            }
        }
    }
}
