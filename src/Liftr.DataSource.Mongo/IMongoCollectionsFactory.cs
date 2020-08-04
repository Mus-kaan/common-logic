//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public interface IMongoCollectionsFactory
    {
        Task<IMongoCollection<T>> GetCollectionAsync<T>(string collectionName);

        IMongoCollection<T> GetCollection<T>(string collectionName);

        Task<IMongoCollection<T>> GetOrCreateEntityCollectionAsync<T>(string collectionName) where T : BaseResourceEntity;

        Task<IMongoCollection<CounterEntity>> GetOrCreateCounterEntityCollectionAsync(string collectionName);

        Task<IMongoCollection<EventHubEntity>> GetOrCreateEventHubEntityCollectionAsync(string collectionName);

        Task<IMongoCollection<T>> GetOrCreateMonitoringCollectionAsync<T>(string collectionName) where T : MonitoringBaseEntity, new();

        Task<IMongoCollection<PartnerResourceEntity>> GetOrCreatePartnerResourceEntityCollectionAsync(string collectionName);

        Task DeleteCollectionAsync(string collectionName);
    }
}
