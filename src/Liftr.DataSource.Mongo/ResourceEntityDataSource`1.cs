//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class ResourceEntityDataSource<TResource> : IResourceEntityDataSource<TResource> where TResource : BaseResourceEntity
    {
        protected readonly IMongoCollection<TResource> _collection;
        protected readonly ITimeSource _timeSource;

        public ResourceEntityDataSource(IMongoCollection<TResource> collection, ITimeSource timeSource)
        {
            _collection = collection;
            _timeSource = timeSource;
        }

        public virtual async Task<TResource> AddEntityAsync(TResource entity)
        {
            try
            {
                entity.CreatedUTC = _timeSource.UtcNow;
                await _collection.InsertOneAsync(entity);
                return entity;
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
