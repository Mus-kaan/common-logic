//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Hosting.Contracts;
using Microsoft.Liftr.KeyVault;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class InfraV2VMSSTests
    {
        private readonly ITestOutputHelper _output;

        public InfraV2VMSSTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task VerifyRegionalDataAndComputeCreationAsync()
        {
            var rootUserName = "aksuser";
            var sshPublicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDIoUCnmwyMDFAf0Ia/OnCTR3g9uxp6uxU/"
            + "Sa4VwFEFpOmMH9fUZcSGPMlAZLtXYUrgsNDLDr22wXI8wd8AXQJTxnxmgSISENVVFntC+1WCETQFMZ4BkEeLCGL0s"
            + "CoAEKnWNjlE4qBbZUfkShGCmj50YC9R0zHcqpCbMCz3BjEGrqttlIHaYGKD1v7g2vHEaDj459cqyQw3yBr3l9erS6"
            + "/vJSe5tBtZPimTTUKhLYP+ZXdqldLa/TI7e6hkZHQuMOe2xXCqMfJXp4HtBszIua7bM3rQFlGuBe7+Vv+NzL5wJyy"
            + "y6KnZjoLknnRoeJUSyZE2UtRF6tpkoGu3PhqZBmx7 limingu@Limins-MacBook-Pro.local";

            var shortPartnerName = SdkContext.RandomResourceName("v", 6);
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, Region.USWest2);
            TestCommon.AddCommonTags(context.Tags);

            var globalBaseName = "gbl";
            var globalRGName = context.ResourceGroupName(globalBaseName);
            var dataBaseName = "data";
            var dataRGName = context.ResourceGroupName(dataBaseName);
            var computeBaseName = "comp";
            var computeRGName = context.ResourceGroupName(computeBaseName);

            var model = JsonConvert.DeserializeObject<ComputeTestModel>(File.ReadAllText("ComputeTestModel.json"));
            model.Options.DataBaseName = dataBaseName;
            model.Options.ComputeBaseName = computeBaseName;

            var dataOptions = JsonConvert.DeserializeObject<RegionalDataOptions>(File.ReadAllText("TestDataOptions.json"));
            dataOptions.EnableVNet = false;
            dataOptions.DBSupport = false;

            using (var globalScope = new TestResourceGroupScope(globalRGName))
            using (var regionalDataScope = new TestResourceGroupScope(dataRGName))
            using (var regionalComputeScope = new TestResourceGroupScope(computeRGName))
            {
                var logger = globalScope.Logger;
                try
                {
                    var infra = new InfrastructureV2(regionalDataScope.AzFactory, TestCredentials.KeyVaultClient, regionalDataScope.Logger);
                    var client = regionalDataScope.Client;

                    var ipNamePrefix = context.GenerateCommonName(globalBaseName, noRegion: true);
                    var ipPool = new IPPoolManager(ipNamePrefix, false, regionalDataScope.AzFactory, logger);

                    var gblResources = await infra.CreateOrUpdateGlobalRGAsync(globalBaseName, context, $"{shortPartnerName}-{globalBaseName}.dummy.com", addGlobalDB: false);
                    dataOptions.GlobalKeyVaultResourceId = gblResources.KeyVault.Id;
                    model.Options.GlobalKeyVaultResourceId = gblResources.KeyVault.Id;

                    var regions = new List<Region>() { context.Location };
                    IEnumerable<RegionOptions> regionOptions = GetRegionOptions(regions);

                    await ipPool.ProvisionIPPoolAsync(context.Location, 3, context.Tags, regionOptions);
                    await client.GetOrCreateResourceGroupAsync(context.Location, dataRGName, context.Tags);

                    using (var globalKVValet = new KeyVaultConcierge(gblResources.KeyVault.VaultUri, TestCredentials.KeyVaultClient, logger))
                    {
                        await globalKVValet.SetSecretAsync("SSHUserName", rootUserName, context.Tags);
                        await globalKVValet.SetSecretAsync("SSHPublicKey", sshPublicKey, context.Tags);
                        await globalKVValet.SetSecretAsync("SSHPassword", Guid.NewGuid().ToString(), context.Tags);
                    }

                    var dataResources = await infra.CreateOrUpdateRegionalDataRGAsync(dataBaseName, context, dataOptions, createVNet: true);

                    // Check regional data resources.
                    {
                        var rg = await client.GetResourceGroupAsync(regionalDataScope.ResourceGroupName);
                        Assert.Equal(regionalDataScope.ResourceGroupName, rg.Name);
                        TestCommon.CheckCommonTags(rg.Inner.Tags);

                        var dbs = await client.ListCosmosDBAsync(regionalDataScope.ResourceGroupName);
                        Assert.Empty(dbs);

                        var retrievedTM = await client.GetTrafficManagerAsync(dataResources.TrafficManager.Id);
                        TestCommon.CheckCommonTags(retrievedTM.Inner.Tags);
                    }

                    var vmssResources = await infra.CreateOrUpdateRegionalVMSSRGAsync(
                        context,
                        model.Options,
                        model.VMSS,
                        model.Geneva,
                        TestCredentials.KeyVaultClient,
                        ipPool,
                        enableVNet: false);

                    // Validate VMSS resources
                    {
                        Assert.NotNull(vmssResources);
                        Assert.NotNull(vmssResources.ResourceGroup);
                        Assert.NotNull(vmssResources.VNet);
                        Assert.NotNull(vmssResources.Subnet);
                    }

                    // Same deployment will not throw exception.
                    await infra.CreateOrUpdateRegionalVMSSRGAsync(
                       context,
                       model.Options,
                       model.VMSS,
                       model.Geneva,
                       TestCredentials.KeyVaultClient,
                       ipPool,
                       enableVNet: false);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"{nameof(VerifyRegionalDataAndComputeCreationAsync)} failed.");
                    throw;
                }
            }
        }

        private IEnumerable<RegionOptions> GetRegionOptions(IEnumerable<Region> regions)
        {
            List<RegionOptions> regionOptions = new List<RegionOptions>();

            foreach (var region in regions)
            {
                var regionOption = new RegionOptions
                {
                    Location = region,
                    ComputeBaseName = $"testCompute-{Guid.NewGuid()}",
                    DataBaseName = $"testDb-{Guid.NewGuid()}",
                };
                regionOptions.Add(regionOption);
            }

            return regionOptions;
        }
    }
}
