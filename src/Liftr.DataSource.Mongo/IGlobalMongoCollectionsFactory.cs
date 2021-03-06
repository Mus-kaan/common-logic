//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public interface IGlobalMongoCollectionsFactory : IMongoCollectionsBaseFactory
    {
        IMongoCollection<AgreementResourceEntity> GetOrCreateAgreementEntityCollection(string collectionName);

        IMongoCollection<MarketplaceSaasResourceEntity> GetOrCreateMarketplaceSaasCollection(string collectionName);

        IMongoCollection<MarketplaceRelationshipEntity> GetOrCreateMarketplaceRelationshipEntityCollection(string collectionName);
    }
}
