//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class InftrastructureV1Tests
    {
        private readonly ITestOutputHelper _output;

        public InftrastructureV1Tests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task VerifyInfraV1Async()
        {
            var logger = TestLogger.GetLogger(_output);
            var options = InfraV1Options.FromFile("infrav1.json");
            options.ShortPartnerName = SdkContext.RandomResourceName("ut", 10);
            var context = new NamingContext(options.PartnerName, options.ShortPartnerName, options.Environment, options.Location);
            TestCommon.AddCommonTags(context.Tags);
            var client = new AzureClient(TestCredentials.GetAzure(), TestCredentials.ClientId, TestCredentials.ClientSecret, logger);

            using (var dataScope = new TestResourceGroupScope(client, context.ResourceGroupName(options.DataCoreName)))
            using (var computeScope = new TestResourceGroupScope(client, context.ResourceGroupName(options.ComputeCoreName)))
            {
                var infra = new InftrastructureV1(client, logger);

                // This will take a long time. Be patient. About 6 minutes.
                await infra.CreateDataAndComputeAsync(options, context);

                // Check data resource group.
                {
                    var rg = await client.GetResourceGroupAsync(dataScope.ResourceGroupName);
                    Assert.Equal(dataScope.ResourceGroupName, rg.Name);
                    TestCommon.CheckCommonTags(rg.Inner.Tags);

                    var dbs = await client.ListCosmosDBAsync(dataScope.ResourceGroupName);
                    Assert.Single(dbs);
                    TestCommon.CheckCommonTags(dbs.First().Inner.Tags);

                    var kvs = await client.ListKeyVaultAsync(dataScope.ResourceGroupName);
                    Assert.Single(kvs);
                    var r = kvs.First();
                    TestCommon.CheckCommonTags(r.Inner.Tags);
                    Assert.Equal(2, r.AccessPolicies.Count);

                    // Insert side.
                    {
                        var insertPolicy = r.AccessPolicies.Where(i => i.ObjectId.OrdinalEquals("11f2c714-9364-47dc-a018-fb2ddc0a1a0f")).FirstOrDefault();
                        Assert.Single(insertPolicy.Permissions.Secrets);
                        Assert.Equal(SecretPermissions.Set.ToString(), insertPolicy.Permissions.Secrets[0]);
                    }

                    // Web app side.
                    {
                        var webAppPolicy = r.AccessPolicies.Where(i => !i.ObjectId.OrdinalEquals("11f2c714-9364-47dc-a018-fb2ddc0a1a0f")).FirstOrDefault();
                        Assert.Equal(2, webAppPolicy.Permissions.Secrets.Count);
                        Assert.NotEqual(-1, webAppPolicy.Permissions.Secrets.IndexOf(SecretPermissions.List.ToString()));
                        Assert.NotEqual(-1, webAppPolicy.Permissions.Secrets.IndexOf(SecretPermissions.Get.ToString()));
                    }
                }

                // Check compute resource group.
                {
                    var rg = await client.GetResourceGroupAsync(computeScope.ResourceGroupName);
                    Assert.Equal(computeScope.ResourceGroupName, rg.Name);
                    TestCommon.CheckCommonTags(rg.Inner.Tags);

                    var apps = await client.ListWebAppAsync(computeScope.ResourceGroupName);
                    Assert.Single(apps);
                    TestCommon.CheckCommonTags(apps.First().Inner.Tags);
                }
            }
        }
    }
}
