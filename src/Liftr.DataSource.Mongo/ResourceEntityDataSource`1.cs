//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class ResourceEntityDataSource<TResource> : IResourceEntityDataSource<TResource> where TResource : BaseResourceEntity
    {
        protected readonly IMongoCollection<TResource> _collection;
        protected readonly MongoWaitQueueRateLimiter _rateLimiter;
        protected readonly ITimeSource _timeSource;
        protected readonly bool _enableOptimisticConcurrencyControl;

        public ResourceEntityDataSource(
            IMongoCollection<TResource> collection,
            MongoWaitQueueRateLimiter rateLimiter,
            ITimeSource timeSource,
            bool enableOptimisticConcurrencyControl = false)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _enableOptimisticConcurrencyControl = enableOptimisticConcurrencyControl;
        }

        public virtual async Task<TResource> AddAsync(TResource entity, CancellationToken cancellationToken = default)
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

            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                entity.CreatedUTC = _timeSource.UtcNow;
                entity.LastModifiedUTC = _timeSource.UtcNow;
                await _collection.InsertOneAsync(entity, options: null, cancellationToken: cancellationToken);
                return entity;
            }
            catch (Exception ex) when (ex.IsMongoDuplicatedKeyException())
            {
                throw new DuplicatedKeyException(ex);
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<TResource> GetAsync(string entityId, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);

            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var cursor = await _collection.FindAsync(filter, options: null, cancellationToken: cancellationToken);
                return await cursor.FirstOrDefaultAsync(cancellationToken);
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<bool> ExistAsync(string entityId, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);

            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var count = await _collection.CountDocumentsAsync(filter, options: null, cancellationToken: cancellationToken);
                return count > 0;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<bool> ExistByResourceIdAsync(string resourceId, bool showActiveOnly = true, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.ResourceId, resourceId);

            if (showActiveOnly)
            {
                filter = filter & builder.Eq(u => u.Active, true);
            }

            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var count = await _collection.CountDocumentsAsync(filter, options: null, cancellationToken: cancellationToken);
                return count > 0;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<IEnumerable<TResource>> ListAsync(string resourceId, bool showActiveOnly = true, CancellationToken cancellationToken = default)
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

            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var cursor = await _collection.FindAsync(filter, options: null, cancellationToken: cancellationToken);
                return await cursor.ToListAsync(cancellationToken);
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<IEnumerable<TResource>> ListAsync(bool showActiveOnly = true, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Empty;

            if (showActiveOnly)
            {
                filter &= builder.Eq(u => u.Active, true);
            }

            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var cursor = await _collection.FindAsync(filter, options: null, cancellationToken: cancellationToken);
                return await cursor.ToListAsync(cancellationToken);
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<bool> SoftDeleteAsync(string entityId, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);
            var update = Builders<TResource>.Update
                .Set(u => u.Active, false)
                .Set(u => u.ProvisioningState, ProvisioningState.Deleting)
                .Set(u => u.LastModifiedUTC, _timeSource.UtcNow);

            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var updateResult = await _collection.UpdateOneAsync(filter, update, options: null, cancellationToken: cancellationToken);
                return updateResult.ModifiedCount == 1;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public virtual async Task<bool> DeleteAsync(string entityId, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);

            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var deleteResult = await _collection.DeleteOneAsync(filter, cancellationToken);
                return deleteResult.DeletedCount == 1;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task UpdateAsync(TResource entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entity.EntityId);

            if (_enableOptimisticConcurrencyControl)
            {
                filter &= builder.Eq(u => u.LastModifiedUTC, entity.LastModifiedUTC);
            }

            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                entity.LastModifiedUTC = _timeSource.UtcNow;
                ReplaceOptions options = null;
                var replaceResult = await _collection.ReplaceOneAsync(filter, entity, options, cancellationToken);
                if (replaceResult.ModifiedCount != 1)
                {
                    throw new UpdateConflictException($"The update failed due to conflict. The entity with object Id '{entity.EntityId}' might be deleted. Or the {nameof(entity.LastModifiedUTC)} does not match with '{entity.LastModifiedUTC.ToZuluString()}'.");
                }
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
    }
}
