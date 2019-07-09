//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class ResourceEntityDataSource
    {
        private readonly IMongoCollection<BaseResourceEntity> _collection;

        public ResourceEntityDataSource(IMongoCollection<BaseResourceEntity> collection)
        {
            _collection = collection;
        }

        public virtual async Task AddEntityAsync(BaseResourceEntity entity)
        {
            await _collection.InsertOneAsync(entity);
        }

        public virtual async Task<BaseResourceEntity> GetEntityAsync(string subscriptionId, string resourceGroup, string name)
        {
            var builder = Builders<BaseResourceEntity>.Filter;
            var filter = builder.Eq(u => u.SubscriptionId, subscriptionId) & builder.Eq(u => u.ResourceGroup, resourceGroup) & builder.Eq(u => u.Name, name);
            var cursor = await _collection.FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        public virtual async Task<IEnumerable<BaseResourceEntity>> ListEntitiesAsync(string subscriptionId, string resourceGroup)
        {
            var builder = Builders<BaseResourceEntity>.Filter;
            var filter = builder.Eq(u => u.SubscriptionId, subscriptionId) & builder.Eq(u => u.ResourceGroup, resourceGroup);
            var cursor = await _collection.FindAsync(filter);
            return cursor.ToEnumerable();
        }

        public virtual async Task<bool> DeleteEntityAsync(string subscriptionId, string resourceGroup, string name)
        {
            var builder = Builders<BaseResourceEntity>.Filter;
            var filter = builder.Eq(u => u.SubscriptionId, subscriptionId) & builder.Eq(u => u.ResourceGroup, resourceGroup) & builder.Eq(u => u.Name, name);
            var rst = await _collection.DeleteOneAsync(filter);
            return rst.DeletedCount == 1;
        }
    }
}
