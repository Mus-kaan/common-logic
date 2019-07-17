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
    public sealed class CosmosTests
    {
        private readonly ITestOutputHelper _output;

        public CosmosTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanCreateCosmosDBAsync()
        {
            // This test will normally take about 12 minutes.
            using (var scope = new TestResourceGroupScope("unittest-db-", _output))
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var dbName = SdkContext.RandomResourceName("test-db", 15);
                var created = await client.CreateCosmosDBAsync(TestCommon.Location, scope.ResourceGroupName, dbName, TestCommon.Tags);

                var dbs = await client.ListCosmosDBAsync(scope.ResourceGroupName);
                Assert.Single(dbs);

                var db = dbs.First();
                Assert.Equal(dbName, db.Name);
                TestCommon.CheckCommonTags(db.Inner.Tags);
            }
        }
    }
}
