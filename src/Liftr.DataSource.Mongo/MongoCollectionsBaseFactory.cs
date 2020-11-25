//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
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
    public abstract class MongoCollectionsBaseFactory : IMongoCollectionsBaseFactory
    {
        protected readonly ILogger _logger;
        protected readonly IMongoDatabase _db;
        protected readonly string _dbName;

        protected MongoCollectionsBaseFactory(MongoOptions options, ILogger logger)
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

            _logger.Information("mongoClientSettings.MaxConnectionPoolSize: {MaxConnectionPoolSize}", mongoClientSettings.MaxConnectionPoolSize);
            var maxDBConcurrency = mongoClientSettings.MaxConnectionPoolSize;
            if (maxDBConcurrency > 50)
            {
                maxDBConcurrency = maxDBConcurrency - 10;
            }

            MongoWaitQueueProtector = new MongoWaitQueueRateLimiter(maxDBConcurrency, _logger);
        }

        public MongoWaitQueueRateLimiter MongoWaitQueueProtector { get; }

        public async Task<IMongoCollection<T>> GetCollectionAsync<T>(string collectionName)
        {
            if (await CollectionExistsAsync(_db, collectionName))
            {
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {collectionName} does not exist.");
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            if (CollectionExists(_db, collectionName))
            {
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {collectionName} does not exist.");
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }

        public async Task DeleteCollectionAsync(string collectionName)
        {
            await _db.DropCollectionAsync(collectionName);
        }

        public void DeleteCollection(string collectionName)
        {
            _db.DropCollection(collectionName);
        }

        #region Internal and Private
        protected IMongoCollection<T> CreateCollection<T>(string collectionName)
        {
            if (!CollectionExists(_db, collectionName))
            {
                _db.CreateCollection(collectionName);
                return _db.GetCollection<T>(collectionName);
            }

            _logger.Fatal($"Collection with name {collectionName} does not exist.");
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }

        protected static async Task<bool> CollectionExistsAsync(IMongoDatabase db, string collectionName)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            var filter = new BsonDocument("name", collectionName);

            // filter by collection name
            var collections = await db.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });

            // check for existence
            return await collections.AnyAsync();
        }

        protected static bool CollectionExists(IMongoDatabase db, string collectionName)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            var filter = new BsonDocument("name", collectionName);

            // filter by collection name
            var collections = db.ListCollections(new ListCollectionsOptions { Filter = filter });

            // check for existence
            return collections.Any();
        }
        #endregion
    }
}
