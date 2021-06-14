//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class EventHubTests : LiftrAzureTestBase
    {
        public EventHubTests(ITestOutputHelper output)
            : base(output, useMethodName: true)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicEastUS]
        public async Task CanEventHubAsync()
        {
            var az = Client;
            var namespaceName = SdkContext.RandomResourceName("evn", 15);
            var ehn = await az.GetOrCreateEventHubNamespaceAsync(TestCommon.Location, ResourceGroupName, namespaceName, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.Tags);
            Assert.Equal(namespaceName, ehn.Name);

            // Second deployment will not fail.
            await az.GetOrCreateEventHubNamespaceAsync(TestCommon.Location, ResourceGroupName, namespaceName, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.Tags);

            var hubName = SdkContext.RandomResourceName("hub", 15);
            var eh = await az.GetOrCreateEventHubAsync(TestCommon.Location, ResourceGroupName, namespaceName, hubName, TestCommon.EventHubPartitionCount, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.EventHubConsumerGroups, TestCommon.Tags);
            Assert.Equal(hubName, eh.Name);
            Assert.Equal(TestCommon.EventHubPartitionCount, eh.PartitionIds.Count);
            var actualConsumerGroups = eh.ListConsumerGroups().Select(s => s.Name).ToList();
            Assert.All(TestCommon.EventHubConsumerGroups, item => Assert.Contains(item, actualConsumerGroups));

            // Second deployment will not fail.
            await az.GetOrCreateEventHubAsync(TestCommon.Location, ResourceGroupName, namespaceName, hubName, TestCommon.EventHubPartitionCount, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.EventHubConsumerGroups, TestCommon.Tags);

            var created = await az.GetOrCreateEventHubNamespaceAsync(TestCommon.Location, ResourceGroupName, namespaceName, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.Tags);
            Assert.Equal(namespaceName, created.Name);
            Assert.Equal(hubName, created.ListEventHubs().First().Name);

            // Create another event hub in the same namespace
            var hubName2 = SdkContext.RandomResourceName("hub", 15);
            var eh2 = await az.GetOrCreateEventHubAsync(TestCommon.Location, ResourceGroupName, namespaceName, hubName2, TestCommon.EventHubPartitionCount, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.EventHubConsumerGroups, TestCommon.Tags);
            Assert.Equal(hubName2, eh2.Name);

            var created2 = await az.GetOrCreateEventHubNamespaceAsync(TestCommon.Location, ResourceGroupName, namespaceName, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.Tags);
            Assert.Equal(2, created.ListEventHubs().Count());
        }
    }
}
