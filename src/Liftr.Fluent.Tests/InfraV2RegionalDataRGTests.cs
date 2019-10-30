//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class InfraV2RegionalDataRGTests
    {
        private readonly ITestOutputHelper _output;

        public InfraV2RegionalDataRGTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task VerifyRegionalDataResourceCreationAsync()
        {
            var shortPartnerName = SdkContext.RandomResourceName("v", 6);
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, Region.USWest);
            TestCommon.AddCommonTags(context.Tags);

            var baseName = "data";
            var rgName = context.ResourceGroupName(baseName);

            using (var globalScope = new TestResourceGroupScope(rgName))
            {
                try
                {
                    var infra = new InftrastructureV2(globalScope.AzFactory, globalScope.Logger);
                    var client = globalScope.Client;

                    // This will take a long time. Be patient. About 6 minutes.
                    (var db, var tm, var kv) = await infra.CreateOrUpdateRegionalDataRGAsync(baseName, context, true);

                    // Check global resource group.
                    {
                        var rg = await client.GetResourceGroupAsync(globalScope.ResourceGroupName);
                        Assert.Equal(globalScope.ResourceGroupName, rg.Name);
                        TestCommon.CheckCommonTags(rg.Inner.Tags);

                        var dbs = await client.ListCosmosDBAsync(globalScope.ResourceGroupName);
                        Assert.Single(dbs);
                        TestCommon.CheckCommonTags(dbs.First().Inner.Tags);

                        var retrievedTM = await client.GetTrafficManagerAsync(tm.Id);
                        TestCommon.CheckCommonTags(retrievedTM.Inner.Tags);
                    }

                    // Same deployment will not throw exception.
                    await infra.CreateOrUpdateRegionalDataRGAsync(baseName, context, true);
                }
                catch (Exception ex)
                {
                    globalScope.Logger.Error(ex, $"{nameof(VerifyRegionalDataResourceCreationAsync)} failed.");
                    throw;
                }
            }
        }
    }
}
