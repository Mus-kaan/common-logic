//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class AksTests
    {
        private readonly ITestOutputHelper _output;

        public AksTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task CanCreateAksAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-aks-", _output))
            {
                try
                {
                    var client = scope.Client;
                    var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("test-aks-", 15);
                    var rootUserName = "aksuser";
                    var sshPublicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDIoUCnmwyMDFAf0Ia/OnCTR3g9uxp6uxU/"
                    + "Sa4VwFEFpOmMH9fUZcSGPMlAZLtXYUrgsNDLDr22wXI8wd8AXQJTxnxmgSISENVVFntC+1WCETQFMZ4BkEeLCGL0s"
                    + "CoAEKnWNjlE4qBbZUfkShGCmj50YC9R0zHcqpCbMCz3BjEGrqttlIHaYGKD1v7g2vHEaDj459cqyQw3yBr3l9erS6"
                    + "/vJSe5tBtZPimTTUKhLYP+ZXdqldLa/TI7e6hkZHQuMOe2xXCqMfJXp4HtBszIua7bM3rQFlGuBe7+Vv+NzL5wJyy"
                    + "y6KnZjoLknnRoeJUSyZE2UtRF6tpkoGu3PhqZBmx7 limingu@Limins-MacBook-Pro.local";
                    var vmCount = 3;

                    await Assert.ThrowsAsync<ArgumentException>(async () =>
                    {
                        await client.CreateAksClusterAsync(
                        TestCommon.Location,
                        scope.ResourceGroupName,
                        name,
                        rootUserName,
                        sshPublicKey,
                        ContainerServiceVMSizeTypes.StandardDS2V2,
                        vmCount,
                        TestCommon.Tags,
                        agentPoolProfileName: "sp-dev");
                    });

                    var created = await client.CreateAksClusterAsync(
                        TestCommon.Location,
                        scope.ResourceGroupName,
                        name,
                        rootUserName,
                        sshPublicKey,
                        ContainerServiceVMSizeTypes.StandardDS2V2,
                        vmCount,
                        TestCommon.Tags,
                        agentPoolProfileName: "spdev");

                    var resources = await client.ListAksClusterAsync(scope.ResourceGroupName);
                    Assert.Single(resources);

                    var k8sCluster = resources.First();
                    Assert.Equal(name, k8sCluster.Name);
                    TestCommon.CheckCommonTags(k8sCluster.Inner.Tags);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, ex.Message);
                    scope.TimedOperation.FailOperation(ex.Message);
                    scope.SkipDeleteResourceGroup = true;
                    throw;
                }
            }
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task CanCreateAksInVNetAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-aks-vnet-", _output))
            {
                try
                {
                    var client = scope.Client;
                    var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("test-aks-", 15);
                    var vnetName = SdkContext.RandomResourceName("vnet", 9);
                    var rootUserName = "aksuser";
                    var sshPublicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDIoUCnmwyMDFAf0Ia/OnCTR3g9uxp6uxU/"
                    + "Sa4VwFEFpOmMH9fUZcSGPMlAZLtXYUrgsNDLDr22wXI8wd8AXQJTxnxmgSISENVVFntC+1WCETQFMZ4BkEeLCGL0s"
                    + "CoAEKnWNjlE4qBbZUfkShGCmj50YC9R0zHcqpCbMCz3BjEGrqttlIHaYGKD1v7g2vHEaDj459cqyQw3yBr3l9erS6"
                    + "/vJSe5tBtZPimTTUKhLYP+ZXdqldLa/TI7e6hkZHQuMOe2xXCqMfJXp4HtBszIua7bM3rQFlGuBe7+Vv+NzL5wJyy"
                    + "y6KnZjoLknnRoeJUSyZE2UtRF6tpkoGu3PhqZBmx7 limingu@Limins-MacBook-Pro.local";
                    var vmCount = 3;

                    var vnet = await client.GetOrCreateVNetAsync(TestCommon.Location, scope.ResourceGroupName, vnetName, TestCommon.Tags);
                    var subnet1 = await client.CreateNewSubnetAsync(vnet, "subnet1");

                    var created = await client.CreateAksClusterAsync(
                    TestCommon.Location,
                    scope.ResourceGroupName,
                    name,
                    rootUserName,
                    sshPublicKey,
                    ContainerServiceVMSizeTypes.StandardDS2V2,
                    vmCount,
                    TestCommon.Tags,
                    subnet: subnet1,
                    agentPoolProfileName: "spdev");

                    var resources = await client.ListAksClusterAsync(scope.ResourceGroupName);
                    Assert.Single(resources);

                    var k8sCluster = resources.First();
                    Assert.Equal(name, k8sCluster.Name);
                    TestCommon.CheckCommonTags(k8sCluster.Inner.Tags);

                    var aksMIObjectId = await client.GetAKSMIAsync(scope.ResourceGroupName, name);
                    var mcMIList = await client.ListAKSMCMIAsync(scope.ResourceGroupName, name, TestCommon.Location);
                    Assert.NotNull(aksMIObjectId);
                    Assert.NotEmpty(mcMIList);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, ex.Message);
                    scope.TimedOperation.FailOperation(ex.Message);
                    scope.SkipDeleteResourceGroup = true;
                    throw;
                }
            }
        }
    }
}
