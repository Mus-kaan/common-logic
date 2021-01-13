//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using Serilog;
using System.Runtime.CompilerServices;

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

        public IMongoCollection<AgreementResourceEntity> GetOrCreateAgreementEntityCollection(string collectionName)
        {
            IMongoCollection<AgreementResourceEntity> collection = null;

            if (!CollectionExists(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
                collection = CreateCollection<AgreementResourceEntity>(collectionName);
            }
            else
            {
                collection = GetCollection<AgreementResourceEntity>(collectionName);
            }

            collection = collection.WithReadPreference(new ReadPreference(ReadPreferenceMode.SecondaryPreferred));
            return collection;
        }

        public IMongoCollection<MarketplaceRelationshipEntity> GetOrCreateMarketplaceRelationshipEntityCollection(string collectionName)
        {
            IMongoCollection<MarketplaceRelationshipEntity> collection = null;

            if (!CollectionExists(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
                collection = CreateCollection<MarketplaceRelationshipEntity>(collectionName);

                var recourceIdIx = new CreateIndexModel<MarketplaceRelationshipEntity>(Builders<MarketplaceRelationshipEntity>.IndexKeys.Ascending(item => item.ResourceId), new CreateIndexOptions<MarketplaceRelationshipEntity> { Unique = true });
                collection.Indexes.CreateOne(recourceIdIx);

                var marketplaceSubscriptionIdx = new CreateIndexModel<MarketplaceRelationshipEntity>(Builders<MarketplaceRelationshipEntity>.IndexKeys.Ascending(item => item.MarketplaceSubscription), new CreateIndexOptions<MarketplaceRelationshipEntity> { Unique = false });
                collection.Indexes.CreateOne(marketplaceSubscriptionIdx);
            }
            else
            {
                collection = GetCollection<MarketplaceRelationshipEntity>(collectionName);
            }

            collection = collection.WithReadPreference(new ReadPreference(ReadPreferenceMode.SecondaryPreferred));
            return collection;
        }

        public IMongoCollection<MarketplaceSaasResourceEntity> GetOrCreateMarketplaceSaasCollection(string collectionName)
        {
            IMongoCollection<MarketplaceSaasResourceEntity> collection = null;

            if (!CollectionExists(_db, collectionName))
            {
                _logger.Warning("Creating collection with name {collectionName} ...", collectionName);
                collection = CreateCollection<MarketplaceSaasResourceEntity>(collectionName);

                var marketplaceSubscriptionIdx = new CreateIndexModel<MarketplaceSaasResourceEntity>(Builders<MarketplaceSaasResourceEntity>.IndexKeys.Ascending(item => item.MarketplaceSubscription), new CreateIndexOptions<MarketplaceSaasResourceEntity> { Unique = true });
                collection.Indexes.CreateOne(marketplaceSubscriptionIdx);

                var createdAtIndex = new CreateIndexModel<MarketplaceSaasResourceEntity>(Builders<MarketplaceSaasResourceEntity>.IndexKeys.Descending(item => item.CreatedUTC), new CreateIndexOptions<MarketplaceSaasResourceEntity> { Unique = false });
                collection.Indexes.CreateOne(createdAtIndex);
            }
            else
            {
                collection = GetCollection<MarketplaceSaasResourceEntity>(collectionName);
            }

            // Add read preference to read from the non-home region. The home region is a remote region.
            collection = collection.WithReadPreference(new ReadPreference(ReadPreferenceMode.SecondaryPreferred));
            return collection;
        }
    }
}
