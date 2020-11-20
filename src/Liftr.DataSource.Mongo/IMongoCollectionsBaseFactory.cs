//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public interface IMongoCollectionsBaseFactory
    {
        Task<IMongoCollection<T>> GetCollectionAsync<T>(string collectionName);

        IMongoCollection<T> GetCollection<T>(string collectionName);

        Task DeleteCollectionAsync(string collectionName);
    }
}
