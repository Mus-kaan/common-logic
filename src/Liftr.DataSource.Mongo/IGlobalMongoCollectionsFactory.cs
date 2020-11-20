//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public interface IGlobalMongoCollectionsFactory : IMongoCollectionsBaseFactory
    {
        Task<IMongoCollection<AgreementResourceEntity>> GetOrCreateAgreementEntityCollectionAsync(string collectionName);
    }
}
