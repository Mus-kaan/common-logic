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
    public sealed class MonitoringRelationshipDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MonitoringRelationship> _collectionScope;

        public MonitoringRelationshipDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<MonitoringRelationship>((db, collectionName) =>
            {
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.GetOrCreateMonitoringRelationshipCollectionAsync(collectionName).Result;
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
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
                IMonitoringRelationshipDataSource s = new MonitoringRelationshipDataSource(_collectionScope.Collection, rateLimiter, ts);

                var tenantId = Guid.NewGuid().ToString();
                var authId = $"/subscriptions/{Guid.NewGuid()}/resourceGroups/5e-ms-dev-machines-wus2-rg/providers/Microsoft.Compute/virtualMachines/eventhujahsfu";
                var diagnosticSettingsName = "diagnosticSettingsName";

                var partnerObjectId1 = ObjectId.GenerateNewId().ToString();
                var partnerObjectId2 = ObjectId.GenerateNewId().ToString();

                var monitoredResourceId1 = $"/subscriptions/{Guid.NewGuid()}/resourceGroups/5e-ms-dev-machines-wus2-rg/providers/Microsoft.Compute/virtualMachines/5e-ms-ub1";
                var monitoredResourceId2 = $"/subscriptions/{Guid.NewGuid()}/resourceGroups/5e-ms-dev-machines-wus2-rg/providers/Microsoft.Compute/virtualMachines/5e-ms-ub2";
                var monitoredResourceId3 = $"/subscriptions/{Guid.NewGuid()}/resourceGroups/5e-ms-dev-machines-wus2-rg/providers/Microsoft.Compute/virtualMachines/5e-ms-ub3";
                var monitoredResourceId4 = $"/subscriptions/{Guid.NewGuid()}/resourceGroups/5e-ms-dev-machines-wus2-rg/providers/Microsoft.Compute/virtualMachines/5e-ms-ub4";

                var mockEntity = new MockMonitoringRelationship()
                {
                    PartnerEntityId = partnerObjectId1,
                    MonitoredResourceId = monitoredResourceId1,
                    AuthorizationRuleId = authId,
                    EventhubName = "mockEvhName",
                    TenantId = tenantId,
                    DiagnosticSettingsName = diagnosticSettingsName,
                };

                var retrieved = await s.GetAsync(mockEntity.TenantId, mockEntity.PartnerEntityId, mockEntity.MonitoredResourceId);
                Assert.Null(retrieved);

                // Can add
                await s.AddAsync(mockEntity);

                // Same combination will throw.
                await Assert.ThrowsAsync<DuplicatedKeyException>(async () =>
                {
                    await s.AddAsync(mockEntity);
                });

                // Can retrieve.
                retrieved = await s.GetAsync(mockEntity.TenantId, mockEntity.PartnerEntityId, mockEntity.MonitoredResourceId);

                Assert.Equal(monitoredResourceId1, retrieved.MonitoredResourceId);
                Assert.Equal(partnerObjectId1, retrieved.PartnerEntityId);
                Assert.Equal(tenantId, retrieved.TenantId);
                Assert.Equal(authId, retrieved.AuthorizationRuleId);
                Assert.Equal(mockEntity.EventhubName, retrieved.EventhubName);
                Assert.Equal(diagnosticSettingsName, retrieved.DiagnosticSettingsName);

                // List entity by monitored resource id
                var list = await s.ListByMonitoredResourceAsync(tenantId, monitoredResourceId1);
                Assert.True(list.Count() == 1);

                // List entity by partner object id
                list = await s.ListByPartnerResourceAsync(tenantId, partnerObjectId1);
                Assert.True(list.Count() == 1);

                mockEntity.MonitoredResourceId = monitoredResourceId2;
                await s.AddAsync(mockEntity);

                mockEntity.MonitoredResourceId = monitoredResourceId3;
                await s.AddAsync(mockEntity);

                mockEntity.MonitoredResourceId = monitoredResourceId4;
                await s.AddAsync(mockEntity);

                // List entity by partner object id
                list = await s.ListByPartnerResourceAsync(tenantId, partnerObjectId1);
                Assert.True(list.Count() == 4);

                // Delete
                var deleteCount = await s.DeleteAsync(tenantId, partnerObjectId1, monitoredResourceId2);
                Assert.Equal(1, deleteCount);

                deleteCount = await s.DeleteAsync(tenantId, partnerObjectId1, monitoredResourceId3);
                Assert.Equal(1, deleteCount);

                // delete deleted record will do nothing.
                deleteCount = await s.DeleteAsync(tenantId, partnerObjectId1, monitoredResourceId3);
                Assert.Equal(0, deleteCount);

                // List entity by partner object id
                list = await s.ListByPartnerResourceAsync(tenantId, partnerObjectId1);
                Assert.True(list.Count() == 2);

                mockEntity.MonitoredResourceId = monitoredResourceId1;
                mockEntity.PartnerEntityId = partnerObjectId2;
                await s.AddAsync(mockEntity);

                // List entity by monitored resource id
                list = await s.ListByMonitoredResourceAsync(tenantId, monitoredResourceId1);
                Assert.Equal(2, list.Count());

                // complain about delete empty parameters.
                await Assert.ThrowsAnyAsync<ArgumentNullException>(async () =>
                {
                    await s.DeleteAsync(tenantId);
                });

                // range delete
                deleteCount = await s.DeleteAsync(tenantId, monitoredResourceId: monitoredResourceId1);
                Assert.Equal(2, deleteCount);

                // List entity by monitored resource id
                list = await s.ListByMonitoredResourceAsync(tenantId, monitoredResourceId1);
                Assert.Empty(list);

                // Can handle subscription case
                mockEntity.MonitoredResourceId = "/SUBSCRIPTIONS/60FAD35B-3A47-4CA0-B691-4A789F737CEA".ToLowerInvariant();
                await s.AddAsync(mockEntity);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
