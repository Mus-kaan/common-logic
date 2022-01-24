//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ManagedIdentity.DataSource
{
    public class ManagedIdentityEntityDataSource : ResourceEntityDataSource<ManagedIdentityEntity>, IManagedIdentityEntityDataSource
    {
        public ManagedIdentityEntityDataSource(IMongoCollection<ManagedIdentityEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
            : base(collection, rateLimiter, timeSource)
        {
        }

        public async Task UpsertAsync(ManagedIdentityEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            // Store ResourceId in capital letters so it can be looked up later using `ListAsync` operation
            entity.ResourceId = entity.ResourceId.ToUpperInvariant();

            var builder = Builders<ManagedIdentityEntity>.Filter;
            var filter = builder.Eq(u => u.EntityId, entity.EntityId);

            await _rateLimiter.WaitAsync();
            try
            {
                entity.LastModifiedUTC = _timeSource.UtcNow;
                var replaceResult = await _collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true });
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
    }
}
