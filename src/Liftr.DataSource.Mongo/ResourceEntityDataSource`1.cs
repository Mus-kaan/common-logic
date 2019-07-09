//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class ResourceEntityDataSource<TResource> where TResource : BaseResourceEntity
    {
        protected readonly IMongoCollection<TResource> _collection;

        public ResourceEntityDataSource(IMongoCollection<TResource> collection)
        {
            _collection = collection;
        }

        public virtual async Task AddEntityAsync(TResource entity)
        {
            try
            {
                await _collection.InsertOneAsync(entity);
            }
            catch (Exception ex) when (ex.IsMongoDuplicatedKeyException())
            {
                throw new DuplicatedKeyException(ex);
            }
        }

        public virtual async Task<TResource> GetEntityAsync(string subscriptionId, string resourceGroup, string name)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.SubscriptionId, subscriptionId) & builder.Eq(u => u.ResourceGroup, resourceGroup) & builder.Eq(u => u.Name, name);
            var cursor = await _collection.FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        public virtual async Task<TResource> GetEntityAsync(string name)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.Name, name);
            var cursor = await _collection.FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        public virtual async Task<IList<TResource>> ListEntitiesAsync(string subscriptionId, string resourceGroup)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.SubscriptionId, subscriptionId) & builder.Eq(u => u.ResourceGroup, resourceGroup);
            var cursor = await _collection.FindAsync(filter);
            return cursor.ToEnumerable().ToList();
        }

        public virtual async Task<bool> DeleteEntityAsync(string subscriptionId, string resourceGroup, string name)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.SubscriptionId, subscriptionId) & builder.Eq(u => u.ResourceGroup, resourceGroup) & builder.Eq(u => u.Name, name);
            var rst = await _collection.DeleteOneAsync(filter);
            return rst.DeletedCount == 1;
        }
    }
}
