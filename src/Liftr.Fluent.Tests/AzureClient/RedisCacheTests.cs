//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests.AzureClient
{
    public sealed class RedisCacheTests
    {
        private readonly ITestOutputHelper _output;

        public RedisCacheTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [JenkinsOnly]
        public async Task CanCreateRedisCacheAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-rc-", _output))
            {
                try
                {
                    var azure = scope.Client;
                    var rg = await azure.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("test-cache-", 15);
                    var rc = await azure.CreateRedisCacheAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags, new Dictionary<string, string> { { "maxclients", "256" } });

                    // List
                    {
                        var redisCacheResources = await azure.ListRedisCacheAsync(scope.ResourceGroupName);
                        Assert.Single(redisCacheResources);
                        var r = redisCacheResources.First();
                        Assert.Equal(name, r.Name);
                        Assert.Equal(scope.ResourceGroupName, r.ResourceGroupName);

                        TestCommon.CheckCommonTags(r.Inner.Tags);
                    }

                    var rc2 = await azure.GetOrCreateRedisCacheAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags, new Dictionary<string, string> { { "maxclients", "256" } });

                    Assert.Equal(rc.Id, rc2.Id);
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