//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public interface IMongoCollectionsFactory
    {
        Task<IMongoCollection<T>> GetCollectionAsync<T>(string collectionName);

        IMongoCollection<T> GetCollection<T>(string collectionName);

        [Obsolete("Use Mongo Shell to create a collection with a partition key. See more: https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-create-container#create-a-container-using-net-sdk")]
        IMongoCollection<T> CreateEntityCollection<T>(string collectionName) where T : BaseResourceEntity;
    }
}
