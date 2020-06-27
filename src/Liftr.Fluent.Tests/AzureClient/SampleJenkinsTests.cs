//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class SampleJenkinsTests
    {
        private readonly ITestOutputHelper _output;

        public SampleJenkinsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [JenkinsOnly]
        public async Task CanCreateAndDeleteGroupAsync()
        {
            using (var scope = new JenkinsTestResourceGroupScope("jenkins-test-rg-", _output))
            {
                try
                {
                    var client = scope.Client;
                    var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var retrieved = await client.GetResourceGroupAsync(scope.ResourceGroupName);

                    TestCommon.CheckCommonTags(retrieved.Inner.Tags);

                    await client.DeleteResourceGroupAsync(scope.ResourceGroupName);

                    // It is deleted.
                    Assert.Null(await client.GetResourceGroupAsync(scope.ResourceGroupName));
                }
                catch (Exception ex)
                {
                    scope.TimedOperation.FailOperation(ex.Message);
                    scope.Logger.Error(ex, ex.Message);
                    throw;
                }
            }
        }

        [Theory]
        [InlineData("westus")]
        [InlineData("eastus")]
        public async Task VerityTheoryAsync(string location)
        {
            if (TestConstants.IsNonJenkins())
            {
                return;
            }

            using (var scope = new JenkinsTestResourceGroupScope("jenkins-test-rg-", _output))
            {
                scope.TimedOperation.SetContextProperty("TestLocation", location);
                var loc = Region.Create(location);
                try
                {
                    var client = scope.Client;
                    var rg = await client.CreateResourceGroupAsync(loc, scope.ResourceGroupName, TestCommon.Tags);
                    var retrieved = await client.GetResourceGroupAsync(scope.ResourceGroupName);

                    TestCommon.CheckCommonTags(retrieved.Inner.Tags);

                    await client.DeleteResourceGroupAsync(scope.ResourceGroupName);

                    // It is deleted.
                    Assert.Null(await client.GetResourceGroupAsync(scope.ResourceGroupName));
                }
                catch (Exception ex)
                {
                    scope.TimedOperation.FailOperation(ex.Message);
                    scope.Logger.Error(ex, ex.Message);
                    throw;
                }
            }
        }

        [JenkinsOnly]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task CleanUpOldTestRGAsync()
        {
            using (var scope = new JenkinsTestResourceGroupScope("unittest-rg-", _output))
            {
                var client = scope.Client;
                var fireAndForget = client.DeleteResourceGroupWithTagAsync("Creator", "UnitTest", (IReadOnlyDictionary<string, string> tags) =>
                {
                    if (tags.ContainsKey("CreatedAt"))
                    {
                        try
                        {
                            var timeStamp = DateTime.Parse(tags["CreatedAt"], CultureInfo.InvariantCulture);
                            if (timeStamp < DateTime.Now.AddDays(-1))
                            {
                                return true;
                            }
                        }
                        catch
                        {
                        }
                    }

                    return false;
                });

                await Task.Yield();
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }
}
