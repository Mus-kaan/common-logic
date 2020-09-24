//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Encryption;
using Microsoft.Liftr.Logging;
using MongoDB.Bson;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.DataSource.Mongo.Tests.MonitoringSvc
{
    public sealed class PartnerResourceDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<PartnerResourceEntity> _collectionScope;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1004:Avoid calling System.Threading.Tasks.Task<TResult>.Result", Justification = "<Pending>")]
        public PartnerResourceDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
#pragma warning disable CA2000 // Dispose objects before losing scope
            _collectionScope = new TestCollectionScope<PartnerResourceEntity>((db, collectionName) =>
            {
                var collection = collectionFactory.GetOrCreatePartnerResourceEntityCollectionAsync(collectionName).Result;
                return collection;
            });
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task BasicDataSourceUsageAsync()
        {
            var ts = new MockTimeSource();
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            var s = new PartnerResourceDataSource(_collectionScope.Collection, rateLimiter, ts);

            var partnerResourceId = "/subscriptions/b0a321d2-3073-44f0-b012-6e60db53ae22/resourceGroups/ngx-test-sbi0920-eus-rg/providers/Microsoft.Storage/storageAccounts/stngxtestsbi0920eus";
            var partnerCredential = "mockPartnerCredential";
            var resourceType = "mockResourceType";
            IEncryptionMetaData encryptionMetaData = new EncryptionMetaData
            {
                ContentEncryptionIV = Encoding.UTF8.GetBytes("mockContentEncryptionIV"),
                EncryptionAlgorithm = EncryptionAlgorithm.A256CBC,
                EncryptionKeyResourceId = "mockKeyResourceId",
            };

            var mockEntity = new PartnerResourceEntity()
            {
                ResourceId = partnerResourceId,
                EncryptedContent = partnerCredential,
                ResourceType = resourceType,
                EncryptionKeyResourceId = encryptionMetaData.EncryptionKeyResourceId,
                EncryptionAlgorithm = encryptionMetaData.EncryptionAlgorithm,
                ContentEncryptionIV = encryptionMetaData.ContentEncryptionIV,
            };

            // Can add
            await s.AddAsync(mockEntity);

            // Can retrieve.
            var retrieved = await s.GetAsync(mockEntity.EntityId);

            Assert.Equal(partnerResourceId.ToUpperInvariant(), retrieved.ResourceId);
            Assert.Equal(partnerCredential, retrieved.EncryptedContent);
            Assert.Equal(resourceType, retrieved.ResourceType);
            Assert.Equal(encryptionMetaData.ContentEncryptionIV, retrieved.ContentEncryptionIV);
            Assert.Equal(encryptionMetaData.EncryptionAlgorithm, retrieved.EncryptionAlgorithm);
            Assert.Equal(encryptionMetaData.EncryptionKeyResourceId, retrieved.EncryptionKeyResourceId);
            Assert.Null(retrieved.TenantId);
            Assert.Null(retrieved.ETag);
            Assert.Null(retrieved.IngestEndpoint);

            // Get by partner resource id
            var entities = await s.ListAsync(partnerResourceId);
            var retrivedByResourceId = entities.FirstOrDefault();

            Assert.Equal(partnerResourceId.ToUpperInvariant(), retrivedByResourceId.ResourceId);
            Assert.Equal(partnerCredential, retrivedByResourceId.EncryptedContent);
            Assert.Equal(resourceType, retrivedByResourceId.ResourceType);
            Assert.Equal(encryptionMetaData.ContentEncryptionIV, retrivedByResourceId.ContentEncryptionIV);
            Assert.Equal(encryptionMetaData.EncryptionAlgorithm, retrivedByResourceId.EncryptionAlgorithm);
            Assert.Equal(encryptionMetaData.EncryptionKeyResourceId, retrivedByResourceId.EncryptionKeyResourceId);

            mockEntity.EntityId = ObjectId.GenerateNewId().ToString();
            await s.AddAsync(mockEntity);

            mockEntity.EntityId = ObjectId.GenerateNewId().ToString();
            mockEntity.TenantId = Guid.NewGuid().ToString();
            mockEntity.ETag = Guid.NewGuid().ToString();
            mockEntity.IngestEndpoint = "SampleEndpoint";
            await s.AddAsync(mockEntity);

            retrieved = await s.GetAsync(mockEntity.EntityId);
            Assert.Equal(mockEntity.ETag, retrieved.ETag);
            Assert.Equal(mockEntity.IngestEndpoint, retrieved.IngestEndpoint);
            Assert.Equal(mockEntity.EncryptionKeyResourceId, retrieved.EncryptionKeyResourceId);

            // List multiple
            entities = await s.ListAsync(partnerResourceId);
            Assert.Equal(3, entities.Count());

            // Soft delete
            var deleted = await s.SoftDeleteAsync(mockEntity.EntityId);
            Assert.True(deleted);

            entities = await s.ListAsync(partnerResourceId);
            Assert.Equal(2, entities.Count());

            entities = await s.ListAsync(partnerResourceId, showActiveOnly: false);
            Assert.Equal(3, entities.Count());

            retrieved = await s.GetAsync(mockEntity.EntityId);
            Assert.Equal(ProvisioningState.Deleting, retrieved.ProvisioningState);
            Assert.False(retrieved.Active);

            // Test delete
            deleted = await s.DeleteAsync(mockEntity.EntityId);
            Assert.True(deleted);

            entities = await s.ListAsync(partnerResourceId, showActiveOnly: false);
            Assert.Equal(2, entities.Count());
        }
    }
}
