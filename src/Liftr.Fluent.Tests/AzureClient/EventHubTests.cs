//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class EventHubTests
    {
        private readonly ITestOutputHelper _output;

        public EventHubTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task CanEventHubAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-stor-", _output))
            {
                try
                {
                    var az = scope.Client;
                    var rg = await az.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var namespaceName = SdkContext.RandomResourceName("evn", 15);
                    var ehn = await az.GetOrCreateEventHubNamespaceAsync(TestCommon.Location, scope.ResourceGroupName, namespaceName, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.Tags);
                    Assert.Equal(namespaceName, ehn.Name);

                    // Second deployment will not fail.
                    await az.GetOrCreateEventHubNamespaceAsync(TestCommon.Location, scope.ResourceGroupName, namespaceName, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.Tags);

                    var hubName = SdkContext.RandomResourceName("hub", 15);
                    var eh = await az.GetOrCreateEventHubAsync(TestCommon.Location, scope.ResourceGroupName, namespaceName, hubName, TestCommon.EventHubPartitionCount, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.EventHubConsumerGroups, TestCommon.Tags);
                    Assert.Equal(hubName, eh.Name);
                    Assert.Equal(TestCommon.EventHubPartitionCount, eh.PartitionIds.Count);
                    var actualConsumerGroups = eh.ListConsumerGroups().Select(s => s.Name).ToList();
                    Assert.All(TestCommon.EventHubConsumerGroups, item => Assert.Contains(item, actualConsumerGroups));

                    // Second deployment will not fail.
                    await az.GetOrCreateEventHubAsync(TestCommon.Location, scope.ResourceGroupName, namespaceName, hubName, TestCommon.EventHubPartitionCount, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.EventHubConsumerGroups, TestCommon.Tags);

                    var created = await az.GetOrCreateEventHubNamespaceAsync(TestCommon.Location, scope.ResourceGroupName, namespaceName, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.Tags);
                    Assert.Equal(namespaceName, created.Name);
                    Assert.Equal(hubName, created.ListEventHubs().First().Name);

                    // Create another event hub in the same namespace
                    var hubName2 = SdkContext.RandomResourceName("hub", 15);
                    var eh2 = await az.GetOrCreateEventHubAsync(TestCommon.Location, scope.ResourceGroupName, namespaceName, hubName2, TestCommon.EventHubPartitionCount, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.EventHubConsumerGroups, TestCommon.Tags);
                    Assert.Equal(hubName2, eh2.Name);

                    var created2 = await az.GetOrCreateEventHubNamespaceAsync(TestCommon.Location, scope.ResourceGroupName, namespaceName, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.Tags);
                    Assert.Equal(2, created.ListEventHubs().Count());
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed.");
                    throw;
                }
            }
        }
    }
}
