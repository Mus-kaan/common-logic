//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        private readonly bool _logOperation;
        private readonly string _collectionName;
        private readonly Serilog.ILogger _logger;

        public ResourceEntityDataSource(
            IMongoCollection<TResource> collection,
            MongoWaitQueueRateLimiter rateLimiter,
            ITimeSource timeSource,
            bool enableOptimisticConcurrencyControl = false,
            bool logOperation = false)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _enableOptimisticConcurrencyControl = enableOptimisticConcurrencyControl;
            _logOperation = logOperation;
            _collectionName = _collection?.CollectionNamespace?.CollectionName ?? throw new InvalidOperationException("Cannot find collection name");
            _logger = rateLimiter.Logger; // Although this looks hacky, changing required function signature will need lots of down stream change.
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

            var op = _logOperation ? _logger.StartTimedOperation($"{_collectionName}-{nameof(AddAsync)}") : null;
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                entity.CreatedUTC = _timeSource.UtcNow;
                entity.LastModifiedUTC = _timeSource.UtcNow;
                await _collection.InsertOneAsync(entity, options: null, cancellationToken: cancellationToken);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(AddAsync)} failed");
                op?.FailOperation(ex.Message);

                if (ex.IsMongoDuplicatedKeyException())
                {
                    throw new DuplicatedKeyException(ex);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                _rateLimiter.Release();
                op?.Dispose();
            }
        }

        public virtual async Task<TResource> GetAsync(string entityId, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);

            var op = _logOperation ? _logger.StartTimedOperation($"{_collectionName}-{nameof(GetAsync)}") : null;
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var cursor = await _collection.FindAsync(filter, options: null, cancellationToken: cancellationToken);
                return await cursor.FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(GetAsync)} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
                op?.Dispose();
            }
        }

        public virtual async Task<bool> ExistAsync(string entityId, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);

            var op = _logOperation ? _logger.StartTimedOperation($"{_collectionName}-{nameof(ExistAsync)}") : null;
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var count = await _collection.CountDocumentsAsync(filter, options: null, cancellationToken: cancellationToken);
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(ExistAsync)} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
                op?.Dispose();
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

            var op = _logOperation ? _logger.StartTimedOperation($"{_collectionName}-{nameof(ExistByResourceIdAsync)}") : null;
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var count = await _collection.CountDocumentsAsync(filter, options: null, cancellationToken: cancellationToken);
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(ExistByResourceIdAsync)} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
                op?.Dispose();
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
                filter &= builder.Eq(u => u.Active, true);
            }

            return await ListAsync(filter, cancellationToken, showActiveOnly);
        }

        public virtual async Task<IEnumerable<TResource>> ListAsync(bool showActiveOnly = true, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Empty;

            if (showActiveOnly)
            {
                filter &= builder.Eq(u => u.Active, true);
            }

            return await ListAsync(filter, cancellationToken, showActiveOnly);
        }

        public virtual Task<IAsyncCursor<TResource>> ListWithCursorAsync(bool showActiveOnly = true, CancellationToken cancellationToken = default)
        {
            return GetCursorAsync(Builders<TResource>.Filter.Empty, cancellationToken, showActiveOnly);
        }

        public virtual async Task<bool> SoftDeleteAsync(string entityId, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);
            var update = Builders<TResource>.Update
                .Set(u => u.Active, false)
                .Set(u => u.LastModifiedUTC, _timeSource.UtcNow);

            var op = _logOperation ? _logger.StartTimedOperation($"{_collectionName}-{nameof(SoftDeleteAsync)}") : null;
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var updateResult = await _collection.UpdateOneAsync(filter, update, options: null, cancellationToken: cancellationToken);
                return updateResult.ModifiedCount == 1;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(SoftDeleteAsync)} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
                op?.Dispose();
            }
        }

        public virtual async Task<bool> DeleteAsync(string entityId, CancellationToken cancellationToken = default)
        {
            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entityId);

            var op = _logOperation ? _logger.StartTimedOperation($"{_collectionName}-{nameof(DeleteAsync)}") : null;
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var deleteResult = await _collection.DeleteOneAsync(filter, cancellationToken);
                return deleteResult.DeletedCount == 1;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(DeleteAsync)} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
                op?.Dispose();
            }
        }

        public async Task UpdateAsync(TResource entity, CancellationToken cancellationToken = default)
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

            var builder = Builders<TResource>.Filter;
            var filter = builder.Eq(u => u.EntityId, entity.EntityId);

            if (_enableOptimisticConcurrencyControl)
            {
                filter &= builder.Eq(u => u.LastModifiedUTC, entity.LastModifiedUTC);
            }

            var op = _logOperation ? _logger.StartTimedOperation($"{_collectionName}-{nameof(UpdateAsync)}") : null;
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
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(UpdateAsync)} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
                op?.Dispose();
            }
        }

        protected async Task<IEnumerable<TResource>> ListAsync(
            FilterDefinition<TResource> filter,
            CancellationToken cancellationToken,
            bool showActiveOnly = true,
            FindOptions<TResource, TResource> options = null,
            [CallerMemberName] string operationName = "")
        {
            var builder = Builders<TResource>.Filter;

            if (showActiveOnly)
            {
                filter &= builder.Eq(u => u.Active, true);
            }

            var op = _logOperation ? _logger.StartTimedOperation($"{_collectionName}-{operationName}") : null;
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var cursor = await _collection.FindAsync(filter, options: options, cancellationToken: cancellationToken);
                return await cursor.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{operationName} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
                op?.Dispose();
            }
        }

        protected async Task<IAsyncCursor<TResource>> GetCursorAsync(
            FilterDefinition<TResource> filter,
            CancellationToken cancellationToken,
            bool showActiveOnly = true,
            FindOptions<TResource, TResource> options = null,
            [CallerMemberName] string operationName = "")
        {
            var builder = Builders<TResource>.Filter;

            if (showActiveOnly)
            {
                filter &= builder.Eq(u => u.Active, true);
            }

            var op = _logOperation ? _logger.StartTimedOperation($"{_collectionName}-{operationName}") : null;
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                return await _collection.FindAsync(filter, options: options, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{operationName} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
                op?.Dispose();
            }
        }

        protected async Task<long> CountAsync(
            FilterDefinition<TResource> filter,
            CancellationToken cancellationToken,
            bool showActiveOnly = true,
            CountOptions options = null,
            [CallerMemberName] string operationName = "")
        {
            var builder = Builders<TResource>.Filter;

            if (showActiveOnly)
            {
                filter &= builder.Eq(u => u.Active, true);
            }

            var op = _logOperation ? _logger.StartTimedOperation($"{_collectionName}-{operationName}") : null;
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                return await _collection.CountDocumentsAsync(filter, options: options, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{operationName} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
                op?.Dispose();
            }
        }
    }
}
