//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
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

        public virtual async Task<TResource> AddAsync(TResource entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (!string.IsNullOrEmpty(entity.ResourceId))
            {
                // resource Id is case insensitive
                entity.ResourceId = entity.ResourceId.ToUpperInvariant();
            }

            try
            {
                entity.CreatedUTC = _timeSource.UtcNow;
                entity.LastModifiedUTC = _timeSource.UtcNow;
                await _collection.InsertOneAsync(entity);
                return entity;
            }
            catch (Exception ex) when (ex.IsMongoDuplicatedKeyException())
            {
                throw new DuplicatedKeyException(ex);
            }
        }

        public virtual async Task<TResource> GetAsync(string entityId)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);
            var cursor = await _collection.FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        public virtual async Task<IEnumerable<TResource>> ListAsync(string resourceId, bool showActiveOnly = true)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            resourceId = resourceId.ToUpperInvariant();
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.ResourceId, resourceId);

            if (showActiveOnly)
            {
                filter = filter & builder.Eq(u => u.Active, true);
            }

            var cursor = await _collection.FindAsync(filter);
            return await cursor.ToListAsync();
        }

        public virtual async Task<bool> SoftDeleteAsync(string entityId)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);
            var update = Builders<TResource>.Update.Set(u => u.Active, false).Set(u => u.ProvisioningState, ProvisioningState.Deleting);
            var updateResult = await _collection.UpdateOneAsync(filter, update);
            return updateResult.ModifiedCount == 1;
        }

        public virtual async Task<bool> DeleteAsync(string entityId)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);
            var deleteResult = await _collection.DeleteOneAsync(filter);
            return deleteResult.DeletedCount == 1;
        }
    }
}
