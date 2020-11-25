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
    }
}
