//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
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
    }
}
