//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Serilog;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

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

        public IMongoCollection<T> GetOrCreateEntityCollection<T>(string collectionName) where T : BaseResourceEntity
        {
            if (!CollectionExists(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
                var collection = CreateCollection<T>(collectionName);

                var resourceIdIdx = new CreateIndexModel<T>(Builders<T>.IndexKeys.Ascending(item => item.ResourceId), new CreateIndexOptions<T> { Unique = false });
                collection.Indexes.CreateOne(resourceIdIdx);

                return collection;
            }
            else
            {
                return GetCollection<T>(collectionName);
            }
        }

        public IMongoCollection<CounterEntity> GetOrCreateCounterEntityCollection(string collectionName)
        {
            if (!CollectionExists(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
                var collection = CreateCollection<CounterEntity>(collectionName);

                var resourceIdIdx = new CreateIndexModel<CounterEntity>(Builders<CounterEntity>.IndexKeys.Ascending(item => item.CounterKey), new CreateIndexOptions<CounterEntity> { Unique = true });
                collection.Indexes.CreateOne(resourceIdIdx);

                return collection;
            }
            else
            {
                return GetCollection<CounterEntity>(collectionName);
            }
        }

        public IMongoCollection<MarketplaceSaasResourceEntity> GetOrCreateMarketplaceEntityCollection(string collectionName)
        {
            IMongoCollection<MarketplaceSaasResourceEntity> collection = null;

            if (!CollectionExists(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
                collection = CreateCollection<MarketplaceSaasResourceEntity>(collectionName);

                var marketplaceSubscriptionIdx = new CreateIndexModel<MarketplaceSaasResourceEntity>(Builders<MarketplaceSaasResourceEntity>.IndexKeys.Ascending(item => item.MarketplaceSubscription), new CreateIndexOptions<MarketplaceSaasResourceEntity> { Unique = true });
                collection.Indexes.CreateOne(marketplaceSubscriptionIdx);
            }
            else
            {
                collection = GetCollection<MarketplaceSaasResourceEntity>(collectionName);
            }

            var createdAtIndex = new CreateIndexModel<MarketplaceSaasResourceEntity>(Builders<MarketplaceSaasResourceEntity>.IndexKeys.Descending(item => item.CreatedUTC), new CreateIndexOptions<MarketplaceSaasResourceEntity> { Unique = false });
            collection.Indexes.CreateOne(createdAtIndex);
            return collection;
        }

        public IMongoCollection<EventHubEntity> GetOrCreateEventHubEntityCollection(string collectionName)
        {
            if (!CollectionExists(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
                var collection = CreateCollection<EventHubEntity>(collectionName);

                return collection;
            }
            else
            {
                return GetCollection<EventHubEntity>(collectionName);
            }
        }

        public IMongoCollection<StorageEntity> GetOrCreateStorageEntityCollection(string collectionName)
        {
            if (!CollectionExists(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
                var collection = CreateCollection<StorageEntity>(collectionName);

                var resourceIdIdx = new CreateIndexModel<StorageEntity>(Builders<StorageEntity>.IndexKeys.Ascending(item => item.ResourceId), new CreateIndexOptions<StorageEntity> { Unique = true });
                collection.Indexes.CreateOne(resourceIdIdx);

                return collection;
            }
            else
            {
                return GetCollection<StorageEntity>(collectionName);
            }
        }

        public IMongoCollection<T> GetOrCreateMonitoringCollection<T>(string collectionName) where T : MonitoringBaseEntity, new()
        {
            var instance = new T();
            var tenantProperty = typeof(T).GetProperty(nameof(instance.TenantId));
            var bsonElementAttribute = tenantProperty?.CustomAttributes?.FirstOrDefault(attr => attr.AttributeType == typeof(BsonElementAttribute));
            if (bsonElementAttribute == null)
            {
                throw new InvalidOperationException($"Please make sure the it has a {nameof(BsonElementAttribute)} attribute on {nameof(instance.TenantId)}");
            }

            string shardingKeyName = bsonElementAttribute.ConstructorArguments.FirstOrDefault().Value.ToString();
            if (!CollectionExists(_db, collectionName))
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
                    var commandResult = _db.RunCommand(shellCommand);
                }
                catch (MongoCommandException ex)
                {
                    var message = $"Encountered issue when creating collection '{_dbName}.{collectionName}' with shard key '{shardingKeyName}'.";
                    _logger.Fatal(ex, message);
                    throw new InvalidOperationException(message, ex);
                }

                var collection = GetCollection<T>(collectionName);

                var monitorIdIdx = new CreateIndexModel<T>(Builders<T>.IndexKeys.Ascending(item => item.MonitoredResourceId), new CreateIndexOptions<T> { Unique = false });
                collection.Indexes.CreateOne(monitorIdIdx);

                var partnerIdIdx = new CreateIndexModel<T>(Builders<T>.IndexKeys.Ascending(item => item.PartnerEntityId), new CreateIndexOptions<T> { Unique = false });
                collection.Indexes.CreateOne(partnerIdIdx);

                return collection;
            }
            else
            {
                return GetCollection<T>(collectionName);
            }
        }

        public IMongoCollection<PartnerResourceEntity> GetOrCreatePartnerResourceEntityCollection(string collectionName)
        {
            return GetOrCreateEntityCollection<PartnerResourceEntity>(collectionName);
        }
    }
}
