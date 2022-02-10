//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class AksTests : LiftrAzureTestBase
    {
        public AksTests(ITestOutputHelper output)
            : base(output, useMethodName: true)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicEastUS]
        public async Task CanCreateAksAsync()
        {
            try
            {
                var client = Client;
                var name = SdkContext.RandomResourceName("test-aks-", 15);
                var rootUserName = "aksuser";
                var sshPublicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDIoUCnmwyMDFAf0Ia/OnCTR3g9uxp6uxU/"
                + "Sa4VwFEFpOmMH9fUZcSGPMlAZLtXYUrgsNDLDr22wXI8wd8AXQJTxnxmgSISENVVFntC+1WCETQFMZ4BkEeLCGL0s"
                + "CoAEKnWNjlE4qBbZUfkShGCmj50YC9R0zHcqpCbMCz3BjEGrqttlIHaYGKD1v7g2vHEaDj459cqyQw3yBr3l9erS6"
                + "/vJSe5tBtZPimTTUKhLYP+ZXdqldLa/TI7e6hkZHQuMOe2xXCqMfJXp4HtBszIua7bM3rQFlGuBe7+Vv+NzL5wJyy"
                + "y6KnZjoLknnRoeJUSyZE2UtRF6tpkoGu3PhqZBmx7 limingu@Limins-MacBook-Pro.local";
                var aksInfo = new AKSInfo();
                aksInfo.AKSMachineCount = 3;

                var outboundIP = await client.CreatePublicIPAsync(TestCommon.Location, ResourceGroupName, $"test-ip-{Guid.NewGuid()}", new Dictionary<string, string> { { "environment", "test" } }, PublicIPSkuType.Standard);
                var outboundIPId = outboundIP.Id;
                aksInfo.AKSMachineType = ContainerServiceVMSizeTypes.StandardDS2V2;

                await Assert.ThrowsAsync<ArgumentException>(async () =>
                {
                    await client.CreateAksClusterAsync(
                    TestCommon.Location,
                    ResourceGroupName,
                    name,
                    rootUserName,
                    sshPublicKey,
                    aksInfo,
                    outboundIPId,
                    TestCommon.Tags,
                    agentPoolProfileName: "sp-dev");
                });

                outboundIP = await client.CreatePublicIPAsync(TestCommon.Location, ResourceGroupName, $"test-ip-{Guid.NewGuid()}", new Dictionary<string, string> { { "environment", "test" } }, PublicIPSkuType.Standard);
                outboundIPId = outboundIP.Id;

                var created = await client.CreateAksClusterAsync(
                    TestCommon.Location,
                    ResourceGroupName,
                    name,
                    rootUserName,
                    sshPublicKey,
                    aksInfo,
                    outboundIPId,
                    TestCommon.Tags,
                    agentPoolProfileName: "spdev");

                Assert.Equal(ManagedClusterSKUTier.Paid, created.Inner.Sku.Tier);

                var resources = await client.ListAksClusterAsync(ResourceGroupName);
                Assert.Single(resources);

                var k8sCluster = resources.First();
                Assert.Equal(name, k8sCluster.Name);
                TestCommon.CheckCommonTags(k8sCluster.Inner.Tags);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }
        }

        [CheckInValidation(skipLinux: true)]
        [PublicEastUS]
        public async Task CanCreateAksInVNetAsync()
        {
            try
            {
                var client = Client;
                var name = SdkContext.RandomResourceName("test-aks-", 15);
                var vnetName = SdkContext.RandomResourceName("vnet", 9);
                var rootUserName = "aksuser";
                var sshPublicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDIoUCnmwyMDFAf0Ia/OnCTR3g9uxp6uxU/"
                + "Sa4VwFEFpOmMH9fUZcSGPMlAZLtXYUrgsNDLDr22wXI8wd8AXQJTxnxmgSISENVVFntC+1WCETQFMZ4BkEeLCGL0s"
                + "CoAEKnWNjlE4qBbZUfkShGCmj50YC9R0zHcqpCbMCz3BjEGrqttlIHaYGKD1v7g2vHEaDj459cqyQw3yBr3l9erS6"
                + "/vJSe5tBtZPimTTUKhLYP+ZXdqldLa/TI7e6hkZHQuMOe2xXCqMfJXp4HtBszIua7bM3rQFlGuBe7+Vv+NzL5wJyy"
                + "y6KnZjoLknnRoeJUSyZE2UtRF6tpkoGu3PhqZBmx7 limingu@Limins-MacBook-Pro.local";
                var aksInfo = new AKSInfo();
                aksInfo.AKSMachineType = ContainerServiceVMSizeTypes.StandardDS2V2;
                aksInfo.AKSMachineCount = 3;

                var vnet = await client.GetOrCreateVNetAsync(TestCommon.Location, ResourceGroupName, vnetName, TestCommon.Tags);
                var subnet1 = await client.CreateNewSubnetAsync(vnet, "subnet1");

                var outboundIP = await client.CreatePublicIPAsync(TestCommon.Location, ResourceGroupName, $"test-ip-{Guid.NewGuid()}", new Dictionary<string, string> { { "environment", "test" } }, PublicIPSkuType.Standard);
                var outboundIPId = outboundIP.Id;

                var created = await client.CreateAksClusterAsync(
                TestCommon.Location,
                ResourceGroupName,
                name,
                rootUserName,
                sshPublicKey,
                aksInfo,
                outboundIPId,
                TestCommon.Tags,
                subnet: subnet1,
                agentPoolProfileName: "spdev");

                var resources = await client.ListAksClusterAsync(ResourceGroupName);
                Assert.Single(resources);

                var k8sCluster = resources.First();
                Assert.Equal(name, k8sCluster.Name);
                TestCommon.CheckCommonTags(k8sCluster.Inner.Tags);

                var aksMIObjectId = await client.GetAKSMIAsync(ResourceGroupName, name);
                var mcMIList = await client.ListAKSMCMIAsync(ResourceGroupName, name, TestCommon.Location);
                Assert.NotNull(aksMIObjectId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }
        }

        [CheckInValidation(skipLinux: true)]
        [PublicEastUS]
        public async Task CanCreateAksWithAvailabilityZoneAsync()
        {
            try
            {
                var client = Client;
                var name = SdkContext.RandomResourceName("test-aks-", 15);
                var rootUserName = "aksuser";
                var sshPublicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDIoUCnmwyMDFAf0Ia/OnCTR3g9uxp6uxU/"
                + "Sa4VwFEFpOmMH9fUZcSGPMlAZLtXYUrgsNDLDr22wXI8wd8AXQJTxnxmgSISENVVFntC+1WCETQFMZ4BkEeLCGL0s"
                + "CoAEKnWNjlE4qBbZUfkShGCmj50YC9R0zHcqpCbMCz3BjEGrqttlIHaYGKD1v7g2vHEaDj459cqyQw3yBr3l9erS6"
                + "/vJSe5tBtZPimTTUKhLYP+ZXdqldLa/TI7e6hkZHQuMOe2xXCqMfJXp4HtBszIua7bM3rQFlGuBe7+Vv+NzL5wJyy"
                + "y6KnZjoLknnRoeJUSyZE2UtRF6tpkoGu3PhqZBmx7 limingu@Limins-MacBook-Pro.local";
                var aksInfo = new AKSInfo();
                aksInfo.AKSMachineType = ContainerServiceVMSizeTypes.StandardDS2V2;
                aksInfo.AKSMachineCount = 3;

                var outboundIP = await client.CreatePublicIPAsync(TestCommon.Location, ResourceGroupName, $"test-ip-{Guid.NewGuid()}", new Dictionary<string, string> { { "environment", "test" } }, PublicIPSkuType.Standard);
                var outboundIPId = outboundIP.Id;

                await Assert.ThrowsAsync<ArgumentException>(async () =>
                {
                    await client.CreateAksClusterAsync(
                    TestCommon.Location,
                    ResourceGroupName,
                    name,
                    rootUserName,
                    sshPublicKey,
                    aksInfo,
                    outboundIPId,
                    TestCommon.Tags,
                    agentPoolProfileName: "sp-dev");
                });

                var created = await client.CreateAksClusterAsync(
                    TestCommon.Location,
                    ResourceGroupName,
                    name,
                    rootUserName,
                    sshPublicKey,
                    aksInfo,
                    outboundIPId,
                    TestCommon.Tags,
                    agentPoolProfileName: "spdev",
                    supportAvailabilityZone: true);

                var resources = await client.ListAksClusterAsync(ResourceGroupName);
                Assert.Single(resources);

                var k8sCluster = resources.First();
                Assert.Equal(name, k8sCluster.Name);
                TestCommon.CheckCommonTags(k8sCluster.Inner.Tags);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }
        }

        [CheckInValidation(skipLinux: true)]
        [PublicEastUS]
        public async Task CanCreateAksWithAutoScaleAsync()
        {
            try
            {
                var client = Client;
                var name = SdkContext.RandomResourceName("test-aks-", 15);
                var rootUserName = "aksuser";
                var sshPublicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDIoUCnmwyMDFAf0Ia/OnCTR3g9uxp6uxU/"
                + "Sa4VwFEFpOmMH9fUZcSGPMlAZLtXYUrgsNDLDr22wXI8wd8AXQJTxnxmgSISENVVFntC+1WCETQFMZ4BkEeLCGL0s"
                + "CoAEKnWNjlE4qBbZUfkShGCmj50YC9R0zHcqpCbMCz3BjEGrqttlIHaYGKD1v7g2vHEaDj459cqyQw3yBr3l9erS6"
                + "/vJSe5tBtZPimTTUKhLYP+ZXdqldLa/TI7e6hkZHQuMOe2xXCqMfJXp4HtBszIua7bM3rQFlGuBe7+Vv+NzL5wJyy"
                + "y6KnZjoLknnRoeJUSyZE2UtRF6tpkoGu3PhqZBmx7 limingu@Limins-MacBook-Pro.local";
                var aksInfo = new AKSInfo();
                aksInfo.AKSMachineType = ContainerServiceVMSizeTypes.StandardDS2V2;
                aksInfo.AKSAutoScaleMinCount = 2;
                aksInfo.AKSAutoScaleMaxCount = 4;

                var outboundIP = await client.CreatePublicIPAsync(TestCommon.Location, ResourceGroupName, $"test-ip-{Guid.NewGuid()}", new Dictionary<string, string> { { "environment", "test" } }, PublicIPSkuType.Standard);
                var outboundIPId = outboundIP.Id;

                await Assert.ThrowsAsync<ArgumentException>(async () =>
                {
                    await client.CreateAksClusterAsync(
                    TestCommon.Location,
                    ResourceGroupName,
                    name,
                    rootUserName,
                    sshPublicKey,
                    aksInfo,
                    outboundIPId,
                    TestCommon.Tags,
                    agentPoolProfileName: "sp-dev");
                });

                var created = await client.CreateAksClusterAsync(
                    TestCommon.Location,
                    ResourceGroupName,
                    name,
                    rootUserName,
                    sshPublicKey,
                    aksInfo,
                    outboundIPId,
                    TestCommon.Tags,
                    agentPoolProfileName: "spdev",
                    supportAvailabilityZone: true);

                var resources = await client.ListAksClusterAsync(ResourceGroupName);
                Assert.Single(resources);

                var k8sCluster = resources.First();
                Assert.Equal(name, k8sCluster.Name);
                TestCommon.CheckCommonTags(k8sCluster.Inner.Tags);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }
        }
    }
}
