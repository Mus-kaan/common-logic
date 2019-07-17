//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class WebAppTests
    {
        private readonly ITestOutputHelper _output;

        public WebAppTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanCreateWebAppAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-antares-", _output))
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var name = SdkContext.RandomResourceName("test-web-app-", 15);
                var envName = "Development";
                var created = await client.CreateWebAppAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags, PricingTier.StandardS1, envName);

                var resources = await client.ListWebAppAsync(scope.ResourceGroupName);
                Assert.Single(resources);

                var r = resources.First();
                Assert.Equal(name, r.Name);
                TestCommon.CheckCommonTags(r.Inner.Tags);
            }
        }
    }
}
