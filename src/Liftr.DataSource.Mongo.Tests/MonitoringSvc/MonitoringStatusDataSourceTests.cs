//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Microsoft.Liftr.Logging;
using MongoDB.Bson;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.DataSource.Mongo.Tests.MonitoringSvc
{
    public sealed class MonitoringStatusDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MonitoringStatus> _collectionScope;

        public MonitoringStatusDataSourceTests()
        {
            var option = new MockMongoOptions()
            { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };

            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);

            _collectionScope = new TestCollectionScope<MonitoringStatus>((db, collectionName) =>
            {
                var collection = collectionFactory.GetOrCreateMonitoringCollectionAsync<MonitoringStatus>(collectionName)
                    .GetAwaiter().GetResult();

                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task BasicDataSourceUsageAsync()
        {
            try
            {
                var ts = new MockTimeSource();
                using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
                IMonitoringStatusDataSource<MonitoringStatus> source = new MonitoringStatusDataSource(
                    _collectionScope.Collection, rateLimiter, TestLogger.VoidLogger, ts);

                var tenantId1 = Guid.NewGuid().ToString();
                var tenantId2 = Guid.NewGuid().ToString();

                var partnerObjectId1 = ObjectId.GenerateNewId().ToString();
                var partnerObjectId2 = ObjectId.GenerateNewId().ToString();

                var monitoredResourceId1 = $"/subscriptions/{Guid.NewGuid()}/resourceGroups/5e-ms-dev-machines-wus2-rg/providers/Microsoft.Compute/virtualMachines/5e-ms-ub1";
                var monitoredResourceId2 = $"/subscriptions/{Guid.NewGuid()}/resourceGroups/5e-ms-dev-machines-wus2-rg/providers/Microsoft.Compute/virtualMachines/5e-ms-ub2";
                var monitoredResourceId3 = $"/subscriptions/{Guid.NewGuid()}";
                var monitoredResourceId4 = $"/subscriptions/{Guid.NewGuid()}/resourceGroups/5e-ms-dev-machines-wus2-rg/providers/Microsoft.Compute/virtualMachines/5e-ms-ub4";

                var entity = new MonitoringStatus()
                {
                    PartnerEntityId = partnerObjectId1,
                    MonitoredResourceId = monitoredResourceId1,
                    TenantId = tenantId1,
                    IsMonitored = true,
                    Reason = "Captured",
                };

                // Collection is initially empty
                var retrievedList = await source.ListByPartnerResourceAsync(tenantId1, partnerObjectId1);
                Assert.Empty(retrievedList);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId2, partnerObjectId2);
                Assert.Empty(retrievedList);

                // Can add
                await source.AddOrUpdateAsync(entity);

                // Can retrieve
                var retrievedEntity = await source.GetAsync(tenantId1, partnerObjectId1, monitoredResourceId1);
                Assert.Equal(monitoredResourceId1, retrievedEntity.MonitoredResourceId);
                Assert.Equal(partnerObjectId1, retrievedEntity.PartnerEntityId);
                Assert.Equal(tenantId1, retrievedEntity.TenantId);
                Assert.True(retrievedEntity.IsMonitored);
                Assert.Equal("Captured", retrievedEntity.Reason);

                // Can update
                entity.IsMonitored = false;
                entity.Reason = "UnsupportedLocation";
                await source.AddOrUpdateAsync(entity);

                retrievedEntity = await source.GetAsync(tenantId1, partnerObjectId1, monitoredResourceId1);
                Assert.Equal(monitoredResourceId1, retrievedEntity.MonitoredResourceId);
                Assert.Equal(partnerObjectId1, retrievedEntity.PartnerEntityId);
                Assert.Equal(tenantId1, retrievedEntity.TenantId);
                Assert.False(retrievedEntity.IsMonitored);
                Assert.Equal("UnsupportedLocation", retrievedEntity.Reason);

                // List entities for partner one
                retrievedList = await source.ListByPartnerResourceAsync(tenantId1, partnerObjectId1);
                Assert.Single(retrievedList);

                // Add two new entities and ensure they are listed
                entity.MonitoredResourceId = monitoredResourceId2;
                await source.AddOrUpdateAsync(entity);
                entity.MonitoredResourceId = monitoredResourceId3;
                await source.AddOrUpdateAsync(entity);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId1, partnerObjectId1);
                Assert.True(retrievedList.Count() == 3);

                // Add new entity for partnerObjectId2 and ensure there are no cross-references
                entity.PartnerEntityId = partnerObjectId2;
                entity.TenantId = tenantId2;
                entity.MonitoredResourceId = monitoredResourceId4;
                await source.AddOrUpdateAsync(entity);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId1, partnerObjectId1);
                Assert.True(retrievedList.Count() == 3);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId1, partnerObjectId2);
                Assert.Empty(retrievedList);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId2, partnerObjectId1);
                Assert.Empty(retrievedList);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId2, partnerObjectId2);
                Assert.Single(retrievedList);

                // Delete non-existing entity
                var deleteCount = await source.DeleteAsync(tenantId1, partnerObjectId1, monitoredResourceId4);
                Assert.Equal(0, deleteCount);

                // Delete existing entity
                deleteCount = await source.DeleteAsync(tenantId1, partnerObjectId1, monitoredResourceId3);
                Assert.Equal(1, deleteCount);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId1, partnerObjectId1);
                Assert.True(retrievedList.Count() == 2);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId2, partnerObjectId2);
                Assert.Single(retrievedList);

                // Range delete
                deleteCount = await source.DeleteAsync(tenantId2, monitoredResourceId: monitoredResourceId4);
                Assert.Equal(1, deleteCount);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId1, partnerObjectId1);
                Assert.True(retrievedList.Count() == 2);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId2, partnerObjectId2);
                Assert.Empty(retrievedList);

                deleteCount = await source.DeleteAsync(tenantId1, partnerObjectId1);
                Assert.Equal(2, deleteCount);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId1, partnerObjectId1);
                Assert.Empty(retrievedList);

                retrievedList = await source.ListByPartnerResourceAsync(tenantId2, partnerObjectId2);
                Assert.Empty(retrievedList);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
