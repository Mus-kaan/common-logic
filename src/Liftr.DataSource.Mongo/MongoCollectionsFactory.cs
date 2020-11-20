//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Serilog;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Microsoft.Lift.AzureAsyncOperation.DataSource.Test")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.DataSource.Mongo.Tests")]

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class MongoCollectionsFactory : MongoCollectionsBaseFactory, IMongoCollectionsFactory
    {
        public MongoCollectionsFactory(MongoOptions options, ILogger logger)
            : base(options, logger)
        {
        }

        public async Task<IMongoCollection<T>> GetOrCreateEntityCollectionAsync<T>(string collectionName) where T : BaseResourceEntity
        {
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
#pragma warning disable CS0618 // Type or member is obsolete
                var collection = await CreateCollectionAsync<T>(collectionName);
#pragma warning restore CS0618 // Type or member is obsolete

                var resourceIdIdx = new CreateIndexModel<T>(Builders<T>.IndexKeys.Ascending(item => item.ResourceId), new CreateIndexOptions<T> { Unique = false });
                collection.Indexes.CreateOne(resourceIdIdx);

                return collection;
            }
            else
            {
                return await GetCollectionAsync<T>(collectionName);
            }
        }

        public async Task<IMongoCollection<CounterEntity>> GetOrCreateCounterEntityCollectionAsync(string collectionName)
        {
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
#pragma warning disable CS0618 // Type or member is obsolete
                var collection = await CreateCollectionAsync<CounterEntity>(collectionName);
#pragma warning restore CS0618 // Type or member is obsolete

                var resourceIdIdx = new CreateIndexModel<CounterEntity>(Builders<CounterEntity>.IndexKeys.Ascending(item => item.CounterKey), new CreateIndexOptions<CounterEntity> { Unique = true });
                collection.Indexes.CreateOne(resourceIdIdx);

                return collection;
            }
            else
            {
                return await GetCollectionAsync<CounterEntity>(collectionName);
            }
        }

        public async Task<IMongoCollection<MarketplaceSaasResourceEntity>> GetOrCreateMarketplaceEntityCollectionAsync(string collectionName)
        {
            IMongoCollection<MarketplaceSaasResourceEntity> collection = null;

            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
#pragma warning disable CS0618 // Type or member is obsolete
                collection = await CreateCollectionAsync<MarketplaceSaasResourceEntity>(collectionName);
#pragma warning restore CS0618 // Type or member is obsolete

                var marketplaceSubscriptionIdx = new CreateIndexModel<MarketplaceSaasResourceEntity>(Builders<MarketplaceSaasResourceEntity>.IndexKeys.Ascending(item => item.MarketplaceSubscription), new CreateIndexOptions<MarketplaceSaasResourceEntity> { Unique = true });
                collection.Indexes.CreateOne(marketplaceSubscriptionIdx);
            }
            else
            {
                collection = await GetCollectionAsync<MarketplaceSaasResourceEntity>(collectionName);
            }

            var createdAtIndex = new CreateIndexModel<MarketplaceSaasResourceEntity>(Builders<MarketplaceSaasResourceEntity>.IndexKeys.Descending(item => item.CreatedUTC), new CreateIndexOptions<MarketplaceSaasResourceEntity> { Unique = false });
            collection.Indexes.CreateOne(createdAtIndex);
            return collection;
        }

        public async Task<IMongoCollection<EventHubEntity>> GetOrCreateEventHubEntityCollectionAsync(string collectionName)
        {
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
#pragma warning disable CS0618 // Type or member is obsolete
                var collection = await CreateCollectionAsync<EventHubEntity>(collectionName);
#pragma warning restore CS0618 // Type or member is obsolete

                return collection;
            }
            else
            {
                return await GetCollectionAsync<EventHubEntity>(collectionName);
            }
        }

        public async Task<IMongoCollection<StorageEntity>> GetOrCreateStorageEntityCollectionAsync(string collectionName)
        {
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
#pragma warning disable CS0618 // Type or member is obsolete
                var collection = await CreateCollectionAsync<StorageEntity>(collectionName);
#pragma warning restore CS0618 // Type or member is obsolete

                var resourceIdIdx = new CreateIndexModel<StorageEntity>(Builders<StorageEntity>.IndexKeys.Ascending(item => item.ResourceId), new CreateIndexOptions<StorageEntity> { Unique = true });
                collection.Indexes.CreateOne(resourceIdIdx);

                return collection;
            }
            else
            {
                return await GetCollectionAsync<StorageEntity>(collectionName);
            }
        }

        public async Task<IMongoCollection<T>> GetOrCreateMonitoringCollectionAsync<T>(string collectionName) where T : MonitoringBaseEntity, new()
        {
            var instance = new T();
            var tenantProperty = typeof(T).GetProperty(nameof(instance.TenantId));
            var bsonElementAttribute = tenantProperty?.CustomAttributes?.FirstOrDefault(attr => attr.AttributeType == typeof(BsonElementAttribute));
            if (bsonElementAttribute == null)
            {
                throw new InvalidOperationException($"Please make sure the it has a {nameof(BsonElementAttribute)} attribute on {nameof(instance.TenantId)}");
            }

            string shardingKeyName = bsonElementAttribute.ConstructorArguments.FirstOrDefault().Value.ToString();
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
                var bson = new BsonDocument
                {
                    { "shardCollection", _dbName + "." + collectionName },
                    { "key", new BsonDocument(shardingKeyName, "hashed") },
                };

                var shellCommand = new BsonDocumentCommand<BsonDocument>(bson);

                try
                {
                    var commandResult = await _db.RunCommandAsync(shellCommand);
                }
                catch (MongoCommandException ex)
                {
                    var message = $"Encountered issue when creating collection '{_dbName}.{collectionName}' with shard key '{shardingKeyName}'.";
                    _logger.Fatal(ex, message);
                    throw new InvalidOperationException(message, ex);
                }

                var collection = await GetCollectionAsync<T>(collectionName);

                var monitorIdIdx = new CreateIndexModel<T>(Builders<T>.IndexKeys.Ascending(item => item.MonitoredResourceId), new CreateIndexOptions<T> { Unique = false });
                collection.Indexes.CreateOne(monitorIdIdx);

                var partnerIdIdx = new CreateIndexModel<T>(Builders<T>.IndexKeys.Ascending(item => item.PartnerEntityId), new CreateIndexOptions<T> { Unique = false });
                collection.Indexes.CreateOne(partnerIdIdx);

                return collection;
            }
            else
            {
                return await GetCollectionAsync<T>(collectionName);
            }
        }

        public Task<IMongoCollection<PartnerResourceEntity>> GetOrCreatePartnerResourceEntityCollectionAsync(string collectionName)
        {
            return GetOrCreateEntityCollectionAsync<PartnerResourceEntity>(collectionName);
        }

        #region Internal and Private
        private async Task<IMongoCollection<T>> CreateCollectionAsync<T>(string collectionName)
        {
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _db.CreateCollection(collectionName);
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {collectionName} does not exist.");
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }

        private IMongoCollection<T> CreateCollection<T>(string collectionName)
        {
            if (!CollectionExists(_db, collectionName))
            {
                _db.CreateCollection(collectionName);
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {collectionName} does not exist.");
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }
        #endregion
    }
}
