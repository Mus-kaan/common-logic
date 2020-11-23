//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts.AzureMonitor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class MetricAlertTests
    {
        private readonly ITestOutputHelper _output;

        public MetricAlertTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task CreateMetricAlertsAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-stor-", _output))
            {
                try
                {
                    var az = scope.Client;

                    var rg = await az.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);

                    // create an EventHubNamespace
                    var namespaceName = SdkContext.RandomResourceName("evn", 15);
                    var ehn = await az.GetOrCreateEventHubNamespaceAsync(TestCommon.Location, scope.ResourceGroupName, namespaceName, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.Tags);
                    Assert.Equal(namespaceName, ehn.Name);

                    // create an EventHub
                    var hubName = SdkContext.RandomResourceName("hub", 15);
                    var eh = await az.GetOrCreateEventHubAsync(TestCommon.Location, scope.ResourceGroupName, namespaceName, hubName, TestCommon.EventHubPartitionCount, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.EventHubConsumerGroups, TestCommon.Tags);
                    Assert.Equal(hubName, eh.Name);
                    Assert.Equal(TestCommon.EventHubPartitionCount, eh.PartitionIds.Count);
                    var actualConsumerGroups = eh.ListConsumerGroups().Select(s => s.Name).ToList();
                    Assert.All(TestCommon.EventHubConsumerGroups, item => Assert.Contains(item, actualConsumerGroups));

                    var created = await az.GetOrCreateEventHubNamespaceAsync(TestCommon.Location, scope.ResourceGroupName, namespaceName, TestCommon.EventHubThroughputUnits, TestCommon.EventHubMaxThroughputUnits, TestCommon.Tags);
                    Assert.Equal(namespaceName, created.Name);
                    Assert.Equal(hubName, created.ListEventHubs().First().Name);

                    // create an Action Group
                    var subId = TestCredentials.SubscriptionId;
                    var ResourceGroupName = scope.ResourceGroupName;
                    var agName = "liftr-redmond-email";
                    var receiverName = "Email Liftr Redmond";
                    var email = "LiftrDevRedmondUnitTest@microsoft.com"; // dummy email
                    var ag = await az.GetOrUpdateActionGroupAsync(ResourceGroupName, agName, receiverName, email);
                    Assert.Equal(agName, ag.Name);

                    // Second deployment will not fail.
                    await az.GetOrUpdateActionGroupAsync(ResourceGroupName, agName, receiverName, email);

                    // create multiple Metric Alerts defined in a JSON file
                    List<MetricAlertOptions> maOptions = JsonConvert.DeserializeObject<List<MetricAlertOptions>>(File.ReadAllText("AzureClient\\MetricAlertOptionsTests.json"));
                    foreach (MetricAlertOptions mao in maOptions)
                    {
                        mao.ActionGroupResourceId = ag.Id;
                        mao.TargetResourceId = ehn.Id;
                        var ma = await az.GetOrUpdateMetricAlertAsync(ResourceGroupName, mao);
                        Assert.Equal(mao.Name, ma.Name);

                        // Second deployment will not fail.
                        await az.GetOrUpdateMetricAlertAsync(ResourceGroupName, mao);
                    }
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
