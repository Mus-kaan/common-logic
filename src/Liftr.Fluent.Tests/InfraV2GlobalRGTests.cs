//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class InfraV2GlobalRGTests : LiftrAzureTestBase
    {
        private readonly ITestOutputHelper _output;

        public InfraV2GlobalRGTests(ITestOutputHelper output)
            : base(output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS2]
        public async Task VerifyGlobalResourceGroupAsync()
        {
            var shortPartnerName = "v2";
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, Region.USWest);
            TestCommon.AddCommonTags(context.Tags);

            var globalCoreName = SdkContext.RandomResourceName("v", 3);
            var globalRGName = context.ResourceGroupName(globalCoreName);

            using (var globalScope = new TestResourceGroupScope(globalRGName))
            {
                var client = Client;
                var infra = new InfrastructureV2(AzFactory, TestCredentials.KeyVaultClient, globalScope.Logger);

                // This will take a long time. Be patient.
                await infra.CreateOrUpdateGlobalRGAsync(globalCoreName, context, $"{globalCoreName}.dummy.com", addGlobalDB: false);

                // Check global resource group.
                {
                    var rg = await client.GetResourceGroupAsync(globalScope.ResourceGroupName);
                    Assert.Equal(globalScope.ResourceGroupName, rg.Name);
                    TestCommon.CheckCommonTags(rg.Inner.Tags);
                }

                // Same deployment will not throw exception.
                await infra.CreateOrUpdateGlobalRGAsync(globalCoreName, context, $"{globalCoreName}.dummy.com", addGlobalDB: false);
            }
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS2]
        public async Task VerifyGlobalResourceGroupWithoutZonalRedundancyAsync()
        {
            var shortPartnerName = "v2";
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, Region.USWest2);
            TestCommon.AddCommonTags(context.Tags);

            var globalCoreName = SdkContext.RandomResourceName("v", 3);
            var globalRGName = context.ResourceGroupName(globalCoreName);

            using (var globalScope = new TestResourceGroupScope(globalRGName))
            {
                var client = Client;
                var infra = new InfrastructureV2(AzFactory, TestCredentials.KeyVaultClient, globalScope.Logger);

                // This will take a long time. Be patient.
                await infra.CreateOrUpdateGlobalRGAsync(globalCoreName, context, $"{globalCoreName}.dummy.com", addGlobalDB: true, createGlobalDBWithZoneRedundancy: false);

                // Check
                {
                    var dbs = await client.ListCosmosDBAsync(globalScope.ResourceGroupName);
                    Assert.Single(dbs);

                    var db = dbs.First();
                    Assert.Equal(false, db.Inner.Locations[0].IsZoneRedundant);
                }
            }
        }
    }
}
