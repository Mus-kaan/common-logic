//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.DataSource.Mongo.Tests.MonitoringSvc;
using System;
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

        [CheckInValidation(skipLinux: true)]
        public async Task CanCreateCosmosDBAsync()
        {
            using var scope = new TestResourceGroupScope("unittest-db-", _output);
            try
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var dbName = SdkContext.RandomResourceName("test-db", 15);
                (var dbAccount, var conn) = await client.CreateCosmosDBAsync(TestCommon.Location, scope.ResourceGroupName, dbName, TestCommon.Tags);

                // Second deployment will not fail.
                await client.CreateCosmosDBAsync(TestCommon.Location, scope.ResourceGroupName, dbName, TestCommon.Tags);

                var dbs = await client.ListCosmosDBAsync(scope.ResourceGroupName);
                Assert.Single(dbs);

                var db = dbs.First();
                Assert.Equal(dbName, db.Name);
                TestCommon.CheckCommonTags(db.Inner.Tags);

                var option = new MockMongoOptions() { ConnectionString = conn, DatabaseName = "unit-test" };
                var collectionFactory = new MongoCollectionsFactory(option, scope.Logger);
                var collection = collectionFactory.GetOrCreateMonitoringCollection<MonitoringRelationship>("montoring-relationship");
                await MonitoringRelationshipDataSourceTests.RunRelationshipTestAsync(collection);
            }
            catch (Exception ex)
            {
                scope.Logger.Error(ex, "test failed");
                throw;
            }
        }

        [CheckInValidation(skipLinux: true)]
        public async Task CanCreateCosmosDBInVNetAsync()
        {
            using var scope = new TestResourceGroupScope("unittest-db-", _output);
            try
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var vnet = await client.GetOrCreateVNetAsync(TestCommon.Location, scope.ResourceGroupName, SdkContext.RandomResourceName("test-vnet", 15), TestCommon.Tags);
                var subnet = vnet.Subnets[client.DefaultSubnetName];
                var dbName = SdkContext.RandomResourceName("test-db", 15);
                (var dbAccount, var conn) = await client.CreateCosmosDBAsync(TestCommon.Location, scope.ResourceGroupName, dbName, TestCommon.Tags, subnet);

                // Second deployment will not fail.
                await client.CreateCosmosDBAsync(TestCommon.Location, scope.ResourceGroupName, dbName, TestCommon.Tags);

                var dbs = await client.ListCosmosDBAsync(scope.ResourceGroupName);
                Assert.Single(dbs);

                var db = dbs.First();
                Assert.Equal(dbName, db.Name);
                TestCommon.CheckCommonTags(db.Inner.Tags);
            }
            catch (Exception ex)
            {
                scope.Logger.Error(ex, "test failed");
                throw;
            }
        }
    }
}
