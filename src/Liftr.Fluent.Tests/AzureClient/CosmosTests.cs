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
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class CosmosTests : LiftrAzureTestBase
    {
        public CosmosTests(ITestOutputHelper output)
            : base(output, useMethodName: true)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS3]
        public async Task CanCreateCosmosDBAsync()
        {
            var client = Client;
            var dbName = SdkContext.RandomResourceName("test-db", 15);
            (var dbAccount, var conn) = await client.CreateCosmosDBAsync(Location, ResourceGroupName, dbName, TestCommon.Tags);

            // Second deployment will not fail.
            var location = Location;
            await client.CreateCosmosDBAsync(location, ResourceGroupName, dbName, TestCommon.Tags);

            var dbs = await client.ListCosmosDBAsync(ResourceGroupName);
            Assert.Single(dbs);

            var db = dbs.First();
            Assert.Equal(dbName, db.Name);
            TestCommon.CheckCommonTags(db.Inner.Tags);
            Assert.Single(db.Inner.Locations);
            Assert.Equal(AvailabilityZoneRegionLookup.HasSupportCosmosDB(location), db.Inner.Locations[0].IsZoneRedundant);

            var option = new MockMongoOptions() { ConnectionString = conn, DatabaseName = "unit-test" };
            var collectionFactory = new MongoCollectionsFactory(option, Logger);
            var collection = collectionFactory.GetOrCreateMonitoringCollection<MonitoringRelationship>("montoring-relationship");
            await MonitoringRelationshipDataSourceTests.RunRelationshipTestAsync(collection);
        }

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS3]
        public async Task RotateCosmosDBConnectionStringAsync()
        {
            var client = Client;
            var dbName = SdkContext.RandomResourceName("test-db", 15);
            (var db, _) = await client.CreateCosmosDBAsync(Location, ResourceGroupName, dbName, TestCommon.Tags);

            var ts = new MockTimeSource();
            var keys1 = await db.GetConnectionStringsAsync();
            var rotationManager = new CosmosDBCredentialLifeCycleManager(db, ts, Logger);

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

        [CheckInValidation(skipLinux: true)]
        [PublicWestUS3]
        public async Task CanCreateCosmosDBInVNetAsync()
        {
            var client = Client;
            var vnet = await client.GetOrCreateVNetAsync(Location, ResourceGroupName, SdkContext.RandomResourceName("test-vnet", 15), TestCommon.Tags);
            var subnet = vnet.Subnets[client.DefaultSubnetName];
            var dbName = SdkContext.RandomResourceName("test-db", 15);
            (var dbAccount, var conn) = await client.CreateCosmosDBAsync(Location, ResourceGroupName, dbName, TestCommon.Tags, subnet);

            // Second deployment will not fail.
            await client.CreateCosmosDBAsync(Location, ResourceGroupName, dbName, TestCommon.Tags);

            var dbs = await client.ListCosmosDBAsync(ResourceGroupName);
            Assert.Single(dbs);

            var db = dbs.First();
            Assert.Equal(dbName, db.Name);
            TestCommon.CheckCommonTags(db.Inner.Tags);
        }
    }
}
