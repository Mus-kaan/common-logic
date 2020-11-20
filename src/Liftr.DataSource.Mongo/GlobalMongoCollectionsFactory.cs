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
    public class GlobalMongoCollectionsFactory : MongoCollectionsBaseFactory, IGlobalMongoCollectionsFactory
    {
        public GlobalMongoCollectionsFactory(MongoOptions options, ILogger logger)
            : base(options, logger)
        {
        }

        public async Task<IMongoCollection<AgreementResourceEntity>> GetOrCreateAgreementEntityCollectionAsync(string collectionName)
        {
            IMongoCollection<AgreementResourceEntity> collection = null;

            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
#pragma warning disable CS0618 // Type or member is obsolete
                collection = await CreateCollectionAsync<AgreementResourceEntity>(collectionName);
#pragma warning restore CS0618 // Type or member is obsolete

                return collection;
            }
            else
            {
                return await GetCollectionAsync<AgreementResourceEntity>(collectionName);
            }
        }

        #region Internal and Private
        private async Task<IMongoCollection<T>> CreateCollectionAsync<T>(string collectionName)
        {
            if (!await CollectionExistsAsync(_db, collectionName))
            {
                _db.CreateCollection(collectionName);
                var collection = _db.GetCollection<T>(collectionName);
                collection = collection.WithReadPreference(new ReadPreference(ReadPreferenceMode.SecondaryPreferred));
                return collection;
            }

            _logger.Fatal($"Collection with name {collectionName} does not exist.");
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }

        private IMongoCollection<T> CreateCollection<T>(string collectionName)
        {
            if (!CollectionExists(_db, collectionName))
            {
                _db.CreateCollection(collectionName);
                var collection = _db.GetCollection<T>(collectionName);
                collection = collection.WithReadPreference(new ReadPreference(ReadPreferenceMode.SecondaryPreferred));
                return collection;
            }

            _logger.Fatal($"Collection with name {collectionName} does not exist.");
            throw new CollectionNotExistException($"Collection with name {collectionName} does not exist.");
        }
        #endregion
    }
}
