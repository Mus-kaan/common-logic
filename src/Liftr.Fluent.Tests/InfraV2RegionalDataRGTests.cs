//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
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
    public sealed class InfraV2RegionalDataRGTests : LiftrAzureTestBase
    {
        private readonly ITestOutputHelper _output;

        public InfraV2RegionalDataRGTests(ITestOutputHelper output)
            : base(output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS]
        public async Task VerifyRegionalDataResourceCreationAsync()
        {
            var shortPartnerName = SdkContext.RandomResourceName("v", 6);
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, Region.USWest);
            TestCommon.AddCommonTags(context.Tags);

            var dps = new List<string> { TestCredentials.SubscriptionId };

            var baseName = "data";
            var rgName = context.ResourceGroupName(baseName);

            var dataOptions = JsonConvert.DeserializeObject<RegionalDataOptions>(File.ReadAllText("TestDataOptions.json"));
            dataOptions.EnableVNet = true;

            using (var regionalDataScope = new TestResourceGroupScope(rgName))
            {
                try
                {
                    var infra = new InfrastructureV2(AzFactory, TestCredentials.KeyVaultClient, regionalDataScope.Logger);
                    var client = Client;

                    await client.GetOrCreateResourceGroupAsync(context.Location, rgName, context.Tags);

                    var globalKv = await TestEnvSetup.SetupGlobalKeyVaultAsync(rgName, client, TestCredentials.KeyVaultClient);
                    dataOptions.GlobalKeyVaultResourceId = globalKv.Id;

                    var resources = await infra.CreateOrUpdateRegionalDataRGAsync(baseName, context, dataOptions, dataOptions.EnableVNet, "Liftr");

                    // Check regional data resources.
                    {
                        var rg = await client.GetResourceGroupAsync(regionalDataScope.ResourceGroupName);
                        Assert.Equal(regionalDataScope.ResourceGroupName, rg.Name);
                        TestCommon.CheckCommonTags(rg.Inner.Tags);

                        var dbs = await client.ListCosmosDBAsync(regionalDataScope.ResourceGroupName);
                        Assert.Single(dbs);
                        var db = dbs.First();
                        TestCommon.CheckCommonTags(db.Inner.Tags);

                        var retrievedTM = await client.GetTrafficManagerAsync(resources.TrafficManager.Id);
                        TestCommon.CheckCommonTags(retrievedTM.Inner.Tags);
                    }

                    // Same deployment will not throw exception.
                    await infra.CreateOrUpdateRegionalDataRGAsync(baseName, context, dataOptions, dataOptions.EnableVNet, "Liftr");
                }
                catch (Exception ex)
                {
                    regionalDataScope.Logger.Error(ex, $"{nameof(VerifyRegionalDataResourceCreationAsync)} failed.");
                    throw;
                }
            }
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS2]
        public async Task VerifyRegionalCosmosDBResourceCreationWithoutZonalRedundancyAsync()
        {
            var shortPartnerName = SdkContext.RandomResourceName("v", 6);
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, Region.USWest2);
            TestCommon.AddCommonTags(context.Tags);

            var baseName = "data";
            var rgName = context.ResourceGroupName(baseName);

            var dataOptions = JsonConvert.DeserializeObject<RegionalDataOptions>(File.ReadAllText("TestDataOptions.json"));
            dataOptions.EnableVNet = true;
            dataOptions.CreateDBWithZoneRedundancy = false;

            using (var regionalDataScope = new TestResourceGroupScope(rgName))
            {
                var infra = new InfrastructureV2(regionalDataScope.AzFactory, TestCredentials.KeyVaultClient, regionalDataScope.Logger);
                var client = regionalDataScope.Client;

                await client.GetOrCreateResourceGroupAsync(context.Location, rgName, context.Tags);

                var globalKv = await TestEnvSetup.SetupGlobalKeyVaultAsync(rgName, client, TestCredentials.KeyVaultClient);
                dataOptions.GlobalKeyVaultResourceId = globalKv.Id;

                var resources = await infra.CreateOrUpdateRegionalDataRGAsync(baseName, context, dataOptions, dataOptions.EnableVNet, "Liftr");

                // Check zone redundancy of cosmos db
                {
                    var dbs = await client.ListCosmosDBAsync(regionalDataScope.ResourceGroupName);
                    Assert.Single(dbs);

                    var db = dbs.First();
                    Assert.Equal(false, db.Inner.Locations[0].IsZoneRedundant);
                }
            }
        }
    }
}
