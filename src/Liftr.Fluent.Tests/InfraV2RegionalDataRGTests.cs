//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
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
    public sealed class InfraV2RegionalDataRGTests
    {
        private readonly ITestOutputHelper _output;

        public InfraV2RegionalDataRGTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild]
        public async Task VerifyRegionalDataResourceCreationAsync()
        {
            var shortPartnerName = SdkContext.RandomResourceName("v", 6);
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, Region.USWest);
            TestCommon.AddCommonTags(context.Tags);

            var dps = new List<string> { TestCredentials.SubscriptionId };

            var baseName = "data";
            var rgName = context.ResourceGroupName(baseName);

            var dataOptions = JsonConvert.DeserializeObject<RegionalDataOptions>(File.ReadAllText("TestDataOptions.json"));
            dataOptions.SSLCert = null;
            dataOptions.FirstPartyCert = null;

            using (var globalScope = new TestResourceGroupScope(rgName))
            {
                try
                {
                    var infra = new InftrastructureV2(globalScope.AzFactory, globalScope.Logger);
                    var client = globalScope.Client;

                    (_, _, var db, var tm, var kv) = await infra.CreateOrUpdateRegionalDataRGAsync(baseName, context, dataOptions, TestCredentials.KeyVaultClient);

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
                    await infra.CreateOrUpdateRegionalDataRGAsync(baseName, context, dataOptions, TestCredentials.KeyVaultClient);
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
