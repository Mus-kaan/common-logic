//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Serilog;
using System;
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

        [Obsolete("Use Mongo Shell to create a collection with a partition key. See more: https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-create-container#create-a-container-using-net-sdk")]
        internal async Task<IMongoCollection<T>> CreateCollectionAsync<T>(string collectionName)
        {
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _db.CreateCollection(collectionName);
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {nameof(collectionName)} does not exist.", collectionName);
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }

        [Obsolete("Use Mongo Shell to create a collection with a partition key. See more: https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-create-container#create-a-container-using-net-sdk")]
        internal IMongoCollection<T> CreateCollection<T>(string collectionName)
        {
            if (!CollectionExists(_db, collectionName))
            {
                _db.CreateCollection(collectionName);
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {nameof(collectionName)} does not exist.", collectionName);
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }

        #region Private
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
