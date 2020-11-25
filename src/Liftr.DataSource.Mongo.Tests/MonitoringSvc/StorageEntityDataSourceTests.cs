//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.DataSource.Mongo.Tests.MonitoringSvc
{
    public sealed class StorageEntityDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<StorageEntity> _collectionScope;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1004:Avoid calling System.Threading.Tasks.Task<TResult>.Result", Justification = "<Pending>")]
        public StorageEntityDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<StorageEntity>((db, collectionName) =>
            {
                var collection = collectionFactory.GetOrCreateStorageEntityCollection(collectionName);
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [CheckInValidation(skipLinux: true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task BasicDataSourceUsageAsync()
        {
            var ts = new MockTimeSource();
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            var s = new StorageEntityDataSource(_collectionScope.Collection, rateLimiter, ts);

            var location1 = "westus";
            var location2 = "East US";
            var location3 = "West US 2";
            var location4 = "East US 2";
            var accountName = "storacistest5e";
            var resourceId = "/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/wuweng-dg-test-wus2-20200921/providers/Microsoft.Storage/storageAccounts/storacistest5e";

            var mockEntity = new StorageEntity()
            {
                AccountName = accountName,
                ResourceId = resourceId,
                LogForwarderRegion = location1,
                StorageRegion = location1,
                Priority = StoragePriority.Primary,
                VNetType = StorageVNetType.PrivateEndpoint,
            };

            await s.AddAsync(mockEntity);

            // Can get the just inserted
            {
                var list = await s.ListAsync(StoragePriority.Primary);
                Assert.Single(list);
                var i = list.First();

                Assert.Equal(mockEntity.AccountName, i.AccountName);
                Assert.Equal(mockEntity.ResourceId, i.ResourceId);
                Assert.Equal(mockEntity.LogForwarderRegion, i.LogForwarderRegion);
                Assert.Equal(mockEntity.StorageRegion, i.StorageRegion);
                Assert.Equal(mockEntity.Priority, i.Priority);
                Assert.Equal(StorageVNetType.PrivateEndpoint, i.VNetType);
                Assert.Equal("v1", i.Version);
                Assert.True(i.IngestionEnabled);
                Assert.True(i.Active);
            }

            try
            {
                await s.AddAsync(mockEntity);
                Assert.False(true, "Same resource Id will throw due to unique index");
            }
            catch (Exception ex) when (ex.Message.OrdinalContains("duplicate key error collection"))
            {
            }

            // check delete
            {
                var list = await s.ListAsync(StoragePriority.Primary);
                Assert.Single(list);
                var i = list.First();

                var deleted = await s.DeleteAsync(i.DocumentObjectId);
                Assert.True(deleted);

                list = await s.ListAsync(StoragePriority.Primary);
                Assert.Empty(list);
            }

            // Add primary
            for (int i = 1; i <= 15; i++)
            {
                mockEntity.AccountName = accountName + location1.NormalizedAzRegion() + i;
                mockEntity.ResourceId = resourceId + location1.NormalizedAzRegion() + i;

                mockEntity.LogForwarderRegion = location1;
                mockEntity.StorageRegion = location1;

                await s.AddAsync(mockEntity);

                mockEntity.AccountName = accountName + location2.NormalizedAzRegion() + i;
                mockEntity.ResourceId = resourceId + location2.NormalizedAzRegion() + i;

                mockEntity.LogForwarderRegion = location2;
                mockEntity.StorageRegion = location2;

                await s.AddAsync(mockEntity);
            }

            // Add backup
            for (int i = 1; i <= 10; i++)
            {
                mockEntity.AccountName = accountName + location3.NormalizedAzRegion() + i;
                mockEntity.ResourceId = resourceId + location3.NormalizedAzRegion() + i;

                mockEntity.LogForwarderRegion = location1;
                mockEntity.StorageRegion = location3;
                mockEntity.Priority = StoragePriority.Backup;

                await s.AddAsync(mockEntity);

                mockEntity.AccountName = accountName + location4.NormalizedAzRegion() + i;
                mockEntity.ResourceId = resourceId + location4.NormalizedAzRegion() + i;

                mockEntity.LogForwarderRegion = location2;
                mockEntity.StorageRegion = location4;
                mockEntity.Priority = StoragePriority.Backup;

                await s.AddAsync(mockEntity);
            }

            // Can get by region
            {
                var list = await s.ListAsync(StoragePriority.Primary, "West US");
                Assert.Equal(15, list.Count());

                list = await s.ListAsync(StoragePriority.Primary, "East US");
                Assert.Equal(15, list.Count());

                list = await s.ListAsync(StoragePriority.Backup, "West US");
                Assert.Equal(10, list.Count());

                list = await s.ListAsync(StoragePriority.Backup, "East US");
                Assert.Equal(10, list.Count());
            }
        }
    }
}
