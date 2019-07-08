//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Serilog;
using System;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public sealed class MongoCollectionsFactory
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
                    clusterConfigurator.Subscribe<CommandSucceededEvent>(e =>
                    {
                        _logger.Verbose("[Mongo | CommandSucceeded] Event :{@CommandSucceededEvent}, Start at UTC: " + DateTime.Now.Subtract(e.Duration).ToZuluString(), e);
                    });

                    clusterConfigurator.Subscribe<CommandFailedEvent>(e =>
                    {
                        _logger.Verbose("[Mongo | CommandFailed] Event :{@CommandFailedEvent}, Start at UTC: " + DateTime.Now.Subtract(e.Duration).ToZuluString(), e);
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

            var msg = $"Collection with name {collectionName} does not exist.";
            _logger.Fatal(msg);
            throw new InvalidOperationException(msg);
        }

        internal async Task<IMongoCollection<T>> CreateCollectionAsync<T>(string collectionName)
        {
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _db.CreateCollection(collectionName);
                return _db.GetCollection<T>(collectionName);
            }

            var msg = $"Collection with name {collectionName} already exist.";
            _logger.Fatal(msg);
            throw new InvalidOperationException(msg);
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
        #endregion
    }
}
