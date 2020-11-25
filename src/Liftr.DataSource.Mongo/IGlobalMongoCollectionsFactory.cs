//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public interface IGlobalMongoCollectionsFactory : IMongoCollectionsBaseFactory
    {
        IMongoCollection<AgreementResourceEntity> GetOrCreateAgreementEntityCollection(string collectionName);
    }
}
