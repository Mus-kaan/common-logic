//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public interface IMongoCollectionsFactory
    {
        Task<IMongoCollection<T>> GetCollectionAsync<T>(string collectionName);

        IMongoCollection<T> GetCollection<T>(string collectionName);
    }
}
