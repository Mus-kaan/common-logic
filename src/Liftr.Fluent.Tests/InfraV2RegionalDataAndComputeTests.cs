//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class InfraV2RegionalDataAndComputeTests
    {
        private readonly ITestOutputHelper _output;

        public InfraV2RegionalDataAndComputeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task VerifyRegionalDataAndComputeCreationAsync()
        {
            var shortPartnerName = SdkContext.RandomResourceName("v", 6);
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, Region.USEast);
            TestCommon.AddCommonTags(context.Tags);

            var dataBaseName = "data";
            var dataRGName = context.ResourceGroupName(dataBaseName);
            var computeBaseName = "comp";
            var computeRGName = context.ResourceGroupName(computeBaseName);

            var model = JsonConvert.DeserializeObject<ComputeTestModel>(File.ReadAllText("ComputeTestModel.json"));
            model.Options.DataBaseName = dataBaseName;
            model.Options.ComputeBaseName = computeBaseName;

            var dataOptions = JsonConvert.DeserializeObject<RegionalDataOptions>(File.ReadAllText("TestDataOptions.json"));
            dataOptions.EnableVNet = true;

            using (var regionalDataScope = new TestResourceGroupScope(dataRGName))
            using (var regionalComputeScope = new TestResourceGroupScope(computeRGName))
            {
                var logger = regionalDataScope.Logger;
                try
                {
                    var infra = new InfrastructureV2(regionalDataScope.AzFactory, TestCredentials.KeyVaultClient, regionalDataScope.Logger);
                    var client = regionalDataScope.Client;

                    await client.GetOrCreateResourceGroupAsync(context.Location, dataRGName, context.Tags);
                    var laName = context.LogAnalyticsName("gbl001");
                    var logAnalytics = await client.GetOrCreateLogAnalyticsWorkspaceAsync(context.Location, dataRGName, laName, context.Tags);
                    dataOptions.LogAnalyticsWorkspaceId = $"/subscriptions/{client.FluentClient.SubscriptionId}/resourcegroups/{dataRGName}/providers/microsoft.operationalinsights/workspaces/{laName}";

                    var resources = await infra.CreateOrUpdateRegionalDataRGAsync(dataBaseName, context, dataOptions, dataOptions.EnableVNet);

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

                    // This will take a long time. Be patient.
                    await infra.CreateOrUpdateRegionalAKSRGAsync(
                        context,
                        model.Options,
                        model.AKS,
                        TestCredentials.KeyVaultClient,
                        enableVNet: false);

                    // Check compute resource group.
                    {
                        var rg = await client.GetResourceGroupAsync(regionalComputeScope.ResourceGroupName);
                        Assert.Equal(regionalComputeScope.ResourceGroupName, rg.Name);
                        TestCommon.CheckCommonTags(rg.Inner.Tags);
                    }

                    // Same deployment will not throw exception.
                    await infra.CreateOrUpdateRegionalAKSRGAsync(
                        context,
                        model.Options,
                        model.AKS,
                        TestCredentials.KeyVaultClient,
                        enableVNet: false);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"{nameof(VerifyRegionalDataAndComputeCreationAsync)} failed.");

                    if (ex is InvalidOperationException
                        && ex.Message.OrdinalContains("Cannot find the kubelet managed identity"))
                    {
                        // swallow exception for flacky AKS MI provisioning time.
                        return;
                    }

                    throw;
                }
            }
        }
    }
}
