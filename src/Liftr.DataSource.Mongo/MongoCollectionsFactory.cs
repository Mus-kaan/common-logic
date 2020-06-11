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
    public class MongoCollectionsFactory : IMongoCollectionsFactory
    {
        private readonly ILogger _logger;
        private readonly IMongoDatabase _db;
        private readonly string _dbName;

        public MongoCollectionsFactory(MongoOptions options, ILogger logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new ArgumentException($"Need valid {nameof(options.ConnectionString)}.");
            }

            if (string.IsNullOrEmpty(options.DatabaseName))
            {
                throw new ArgumentException($"Need valid {nameof(options.DatabaseName)}.");
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(options.ConnectionString));
            mongoClientSettings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };

            if (options.LogDBOperation)
            {
                mongoClientSettings.ClusterConfigurator = clusterConfigurator =>
                {
                    clusterConfigurator.Subscribe<CommandSucceededEvent>(eventData =>
                    {
                        _logger.Debug("[ Mongo | CommandSucceeded ] StartTime: {StartTime}, Event:{@CommandSucceededEvent}.", DateTime.Now.Subtract(eventData.Duration).ToZuluString(), eventData);
                    });

                    clusterConfigurator.Subscribe<CommandFailedEvent>(eventData =>
                    {
                        _logger.Information("[ Mongo | CommandFailed ] StartTime: {StartTime}, Event:{@CommandFailedEvent}.", DateTime.Now.Subtract(eventData.Duration).ToZuluString(), eventData);
                    });
                };
            }

            var client = new MongoClient(mongoClientSettings);
            _db = client.GetDatabase(options.DatabaseName);
            _dbName = options.DatabaseName;
        }

        public async Task<IMongoCollection<T>> GetCollectionAsync<T>(string collectionName)
        {
            if (await CollectionExistsAsync(_db, collectionName))
            {
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {nameof(collectionName)} does not exist.", collectionName);
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            if (CollectionExists(_db, collectionName))
            {
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {nameof(collectionName)} does not exist.", collectionName);
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
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

        public async Task<IMongoCollection<T>> GetOrCreateMarketplaceEntityCollectionAsync<T>(string collectionName) where T : MarketplaceResourceContainerEntity
        {
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
#pragma warning disable CS0618 // Type or member is obsolete
                var collection = await CreateCollectionAsync<T>(collectionName);
#pragma warning restore CS0618 // Type or member is obsolete
                var marketplaceSubscriptionIdx = new CreateIndexModel<T>(Builders<T>.IndexKeys.Ascending(item => item.MarketplaceSaasResource.MarketplaceSubscription), new CreateIndexOptions<T> { Unique = false });
                collection.Indexes.CreateOne(marketplaceSubscriptionIdx);
                var resourceIdIdx = new CreateIndexModel<T>(
                    Builders<T>.IndexKeys.Ascending(item => item.ResourceId),
                    new CreateIndexOptions<T> { Unique = false });
                collection.Indexes.CreateOne(resourceIdIdx);

                return collection;
            }
            else
            {
                return await GetCollectionAsync<T>(collectionName);
            }
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

        public async Task<IMongoCollection<MonitoringRelationship>> GetOrCreateMonitoringRelationshipCollectionAsync(string collectionName)
        {
            var instance = new MonitoringRelationship();
            var tenantProperty = typeof(MonitoringRelationship).GetProperty(nameof(instance.TenantId));
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

                var collection = await GetCollectionAsync<MonitoringRelationship>(collectionName);

                var monitorIdIdx = new CreateIndexModel<MonitoringRelationship>(Builders<MonitoringRelationship>.IndexKeys.Ascending(item => item.MonitoredResourceId), new CreateIndexOptions<MonitoringRelationship> { Unique = false });
                collection.Indexes.CreateOne(monitorIdIdx);

                var partnerIdIdx = new CreateIndexModel<MonitoringRelationship>(Builders<MonitoringRelationship>.IndexKeys.Ascending(item => item.PartnerEntityId), new CreateIndexOptions<MonitoringRelationship> { Unique = false });
                collection.Indexes.CreateOne(partnerIdIdx);

                return collection;
            }
            else
            {
                return await GetCollectionAsync<MonitoringRelationship>(collectionName);
            }
        }

        public Task<IMongoCollection<PartnerResourceEntity>> GetOrCreatePartnerResourceEntityCollectionAsync(string collectionName)
        {
            return GetOrCreateEntityCollectionAsync<PartnerResourceEntity>(collectionName);
        }

        public async Task DeleteCollectionAsync(string collectionName)
        {
            await _db.DropCollectionAsync(collectionName);
        }

        #region Internal and Private
        private async Task<IMongoCollection<T>> CreateCollectionAsync<T>(string collectionName)
        {
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _db.CreateCollection(collectionName);
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {nameof(collectionName)} does not exist.", collectionName);
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }

        private IMongoCollection<T> CreateCollection<T>(string collectionName)
        {
            if (!CollectionExists(_db, collectionName))
            {
                _db.CreateCollection(collectionName);
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {nameof(collectionName)} does not exist.", collectionName);
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }

        private static async Task<bool> CollectionExistsAsync(IMongoDatabase db, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);

            // filter by collection name
            var collections = await db.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });

            // check for existence
            return await collections.AnyAsync();
        }

        private static bool CollectionExists(IMongoDatabase db, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);

            // filter by collection name
            var collections = db.ListCollections(new ListCollectionsOptions { Filter = filter });

            // check for existence
            return collections.Any();
        }
        #endregion
    }
}
