//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
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

        [Fact]
        public async Task CanCreateAksAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-aks-", _output))
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
                var created = await client.CreateAksClusterAsync(
                    TestCommon.Location,
                    scope.ResourceGroupName,
                    name,
                    rootUserName,
                    sshPublicKey,
                    TestCredentials.ClientId,
                    TestCredentials.ClientSecret,
                    ContainerServiceVirtualMachineSizeTypes.StandardDS2V2,
                    vmCount,
                    TestCommon.Tags);

                var resources = await client.ListAksClusterAsync(scope.ResourceGroupName);
                Assert.Single(resources);

                var k8sCluster = resources.First();
                Assert.Equal(name, k8sCluster.Name);
                TestCommon.CheckCommonTags(k8sCluster.Inner.Tags);
            }
        }
    }
}
