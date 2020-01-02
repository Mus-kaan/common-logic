//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class LogAnalyticsTests
    {
        private readonly ITestOutputHelper _output;

        public LogAnalyticsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild]
        public async Task CreateLogAnalyticsWorkspaceAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-log-anal-", _output))
            {
                try
                {
                    var azure = scope.Client;
                    var rg = await azure.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("log-anal-", 15);

                    var helper = new LogAnalyticsHelper(scope.Logger);
                    var getResult = await helper.GetLogAnalyticsWorkspaceAsync(azure, scope.ResourceGroupName, name);
                    Assert.Null(getResult);

                    await helper.CreateLogAnalyticsWorkspaceAsync(azure, TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);
                    getResult = await helper.GetLogAnalyticsWorkspaceAsync(azure, scope.ResourceGroupName, name);
                    Assert.NotNull(getResult);

                    // same operation will not fail.
                    await helper.CreateLogAnalyticsWorkspaceAsync(azure, TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);
                    getResult = await helper.GetLogAnalyticsWorkspaceAsync(azure, scope.ResourceGroupName, name);
                    Assert.NotNull(getResult);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed");
                    throw;
                }
            }
        }

        [SkipInOfficialBuild]
        public async Task CreateLogAnalyticsWorkspace2Async()
        {
            using (var scope = new TestResourceGroupScope("ut-log-anal-", _output))
            {
                try
                {
                    var azure = scope.Client;
                    var rg = await azure.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("log-anal-", 15);

                    var logAnalytics = await azure.GetLogAnalyticsWorkspaceAsync(scope.ResourceGroupName, name);
                    Assert.Null(logAnalytics);

                    logAnalytics = await azure.GetOrCreateLogAnalyticsWorkspaceAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);
                    Assert.NotNull(logAnalytics);

                    // same operation will not fail.
                    logAnalytics = await azure.GetOrCreateLogAnalyticsWorkspaceAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);
                    Assert.NotNull(logAnalytics);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed");
                    throw;
                }
            }
        }
    }
}
