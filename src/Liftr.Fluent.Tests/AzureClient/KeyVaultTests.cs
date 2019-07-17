//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class KeyVaultTests
    {
        private readonly ITestOutputHelper _output;

        public KeyVaultTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanCreateKeyVaultAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-kv-", _output))
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var name = SdkContext.RandomResourceName("test-vault-", 15);
                var created = await client.CreateKeyVaultAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags, TestCredentials.ClientId);

                // List
                {
                    var resources = await client.ListKeyVaultAsync(scope.ResourceGroupName);
                    Assert.Single(resources);
                    var r = resources.First();
                    Assert.Equal(name, r.Name);
                    TestCommon.CheckCommonTags(r.Inner.Tags);

                    Assert.Single(r.AccessPolicies);
                    Assert.Equal("11f2c714-9364-47dc-a018-fb2ddc0a1a0f", r.AccessPolicies[0].ObjectId);
                }

                // Get
                {
                    var r = await client.GetKeyVaultByIdAsync(created.Id);
                    Assert.Equal(name, r.Name);
                    TestCommon.CheckCommonTags(r.Inner.Tags);

                    Assert.Single(r.AccessPolicies);
                    Assert.Equal("11f2c714-9364-47dc-a018-fb2ddc0a1a0f", r.AccessPolicies[0].ObjectId);
                }
            }
        }
    }
}
