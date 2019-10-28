//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class MSITests
    {
        private readonly ITestOutputHelper _output;

        public MSITests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanCreateAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-msi-", _output))
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var name = SdkContext.RandomResourceName("test-msi-", 15);

                var created = await client.CreateMSIAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);
                var retrieved = await client.GetMSIAsync(created.Id);

                Assert.Equal(name, retrieved.Name);
                TestCommon.CheckCommonTags(retrieved.Inner.Tags);
            }
        }
    }
}
