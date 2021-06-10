//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.DataSource.Mongo.Tests.MonitoringSvc;
using Microsoft.Liftr.Fluent.Contracts;
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
                var location = TestCommon.Location;
                await client.CreateCosmosDBAsync(location, scope.ResourceGroupName, dbName, TestCommon.Tags);

                var dbs = await client.ListCosmosDBAsync(scope.ResourceGroupName);
                Assert.Single(dbs);

                var db = dbs.First();
                Assert.Equal(dbName, db.Name);
                TestCommon.CheckCommonTags(db.Inner.Tags);
                Assert.Single(db.Inner.Locations);
                Assert.Equal(AvailabilityZoneRegionLookup.HasSupportCosmosDB(location), db.Inner.Locations[0].IsZoneRedundant);

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
        public async Task RotateCosmosDBConnectionStringAsync()
        {
            using var scope = new TestResourceGroupScope("unittest-db-", _output);
            try
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var dbName = SdkContext.RandomResourceName("test-db", 15);
                (var db, _) = await client.CreateCosmosDBAsync(TestCommon.Location, scope.ResourceGroupName, dbName, TestCommon.Tags);

                var ts = new MockTimeSource();
                var keys1 = await db.GetConnectionStringsAsync();
                var rotationManager = new CosmosDBCredentialLifeCycleManager(db, ts, scope.Logger);

                // The primary is the default active.
                var conn = await rotationManager.GetActiveConnectionStringAsync();
                Assert.Equal(keys1.PrimaryMongoDBConnectionString, conn);

                var connRO = await rotationManager.GetActiveConnectionStringAsync(readOnly: true);
                Assert.Equal(keys1.PrimaryReadOnlyMongoDBConnectionString, connRO);

                ts.Add(TimeSpan.FromDays(2.3));
                {
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    Assert.Equal(keys1.PrimaryMongoDBConnectionString, conn);

                    connRO = await rotationManager.GetActiveConnectionStringAsync(readOnly: true);
                    Assert.Equal(keys1.PrimaryReadOnlyMongoDBConnectionString, connRO);
                }

                ts.Add(TimeSpan.FromDays(2.3));
                {
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    Assert.Equal(keys1.PrimaryMongoDBConnectionString, conn);

                    connRO = await rotationManager.GetActiveConnectionStringAsync(readOnly: true);
                    Assert.Equal(keys1.PrimaryReadOnlyMongoDBConnectionString, connRO);
                }

                // make sure all are not rotated
                var keys = await db.GetConnectionStringsAsync();

                Assert.Equal(keys1.PrimaryMongoDBConnectionString, keys.PrimaryMongoDBConnectionString);
                Assert.Equal(keys1.PrimaryReadOnlyMongoDBConnectionString, keys.PrimaryReadOnlyMongoDBConnectionString);
                Assert.Equal(keys1.SecondaryMongoDBConnectionString, keys.SecondaryMongoDBConnectionString);
                Assert.Equal(keys1.SecondaryReadOnlyMongoDBConnectionString, keys.SecondaryReadOnlyMongoDBConnectionString);

                // This will trigger rotation
                ts.Add(TimeSpan.FromDays(28));
                {
                    // active is secondary.
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    keys = await db.GetConnectionStringsAsync();

                    // primary are not rotated
                    Assert.Equal(keys1.PrimaryMongoDBConnectionString, keys.PrimaryMongoDBConnectionString);
                    Assert.Equal(keys1.PrimaryReadOnlyMongoDBConnectionString, keys.PrimaryReadOnlyMongoDBConnectionString);

                    // secondary are rotated
                    Assert.NotEqual(keys1.SecondaryMongoDBConnectionString, keys.SecondaryMongoDBConnectionString);
                    Assert.NotEqual(keys1.SecondaryReadOnlyMongoDBConnectionString, keys.SecondaryReadOnlyMongoDBConnectionString);

                    Assert.Equal(keys.SecondaryMongoDBConnectionString, conn);

                    connRO = await rotationManager.GetActiveConnectionStringAsync(readOnly: true);
                    Assert.Equal(keys.SecondaryReadOnlyMongoDBConnectionString, connRO);
                }

                var keys2 = await db.GetConnectionStringsAsync();

                // in TTL, not rotate
                ts.Add(TimeSpan.FromDays(2.3));
                {
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    Assert.Equal(keys2.SecondaryMongoDBConnectionString, conn);

                    connRO = await rotationManager.GetActiveConnectionStringAsync(readOnly: true);
                    Assert.Equal(keys2.SecondaryReadOnlyMongoDBConnectionString, connRO);
                }

                // This will trigger rotation again
                ts.Add(TimeSpan.FromDays(28));
                {
                    // active is primary.
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    keys = await db.GetConnectionStringsAsync();

                    // primary are rotated
                    Assert.NotEqual(keys2.PrimaryMongoDBConnectionString, keys.PrimaryMongoDBConnectionString);
                    Assert.NotEqual(keys2.PrimaryReadOnlyMongoDBConnectionString, keys.PrimaryReadOnlyMongoDBConnectionString);

                    // secondary are not rotated
                    Assert.Equal(keys2.SecondaryMongoDBConnectionString, keys.SecondaryMongoDBConnectionString);
                    Assert.Equal(keys2.SecondaryReadOnlyMongoDBConnectionString, keys.SecondaryReadOnlyMongoDBConnectionString);

                    Assert.Equal(keys.PrimaryMongoDBConnectionString, conn);

                    connRO = await rotationManager.GetActiveConnectionStringAsync(readOnly: true);
                    Assert.Equal(keys.PrimaryReadOnlyMongoDBConnectionString, connRO);
                }

                var keys3 = await db.GetConnectionStringsAsync();

                // explicit rotate
                ts.Add(TimeSpan.FromDays(2.3));
                await rotationManager.RotateCredentialAsync();
                {
                    // active is secondary.
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    keys = await db.GetConnectionStringsAsync();

                    // primary are not rotated
                    Assert.Equal(keys3.PrimaryMongoDBConnectionString, keys.PrimaryMongoDBConnectionString);
                    Assert.Equal(keys3.PrimaryReadOnlyMongoDBConnectionString, keys.PrimaryReadOnlyMongoDBConnectionString);

                    // secondary are rotated
                    Assert.NotEqual(keys3.SecondaryMongoDBConnectionString, keys.SecondaryMongoDBConnectionString);
                    Assert.NotEqual(keys3.SecondaryReadOnlyMongoDBConnectionString, keys.SecondaryReadOnlyMongoDBConnectionString);

                    Assert.Equal(keys.SecondaryMongoDBConnectionString, conn);

                    connRO = await rotationManager.GetActiveConnectionStringAsync(readOnly: true);
                    Assert.Equal(keys.SecondaryReadOnlyMongoDBConnectionString, connRO);
                }
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
