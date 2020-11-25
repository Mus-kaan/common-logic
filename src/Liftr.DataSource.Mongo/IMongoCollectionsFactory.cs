//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using MongoDB.Driver;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public interface IMongoCollectionsFactory : IMongoCollectionsBaseFactory
    {
        IMongoCollection<T> GetOrCreateEntityCollection<T>(string collectionName) where T : BaseResourceEntity;

        IMongoCollection<CounterEntity> GetOrCreateCounterEntityCollection(string collectionName);

        IMongoCollection<EventHubEntity> GetOrCreateEventHubEntityCollection(string collectionName);

        IMongoCollection<T> GetOrCreateMonitoringCollection<T>(string collectionName) where T : MonitoringBaseEntity, new();

        IMongoCollection<PartnerResourceEntity> GetOrCreatePartnerResourceEntityCollection(string collectionName);
    }
}
