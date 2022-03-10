//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.VNetInjection.DataSource.Mongo
{
    public class VNetInjectionEntityDataSource : ResourceEntityDataSource<VNetInjectionEntity>, IVNetInjectionEntityDataSource
    {
        public VNetInjectionEntityDataSource(
            IMongoCollection<VNetInjectionEntity> collection,
            MongoWaitQueueRateLimiter rateLimiter,
            ITimeSource timeSource,
            bool enableOptimisticConcurrencyControl = false,
            bool logOperation = false)
            : base(collection, rateLimiter, timeSource, enableOptimisticConcurrencyControl, logOperation)
        {
        }

        public async Task<VNetInjectionEntity> UpsertAsync(VNetInjectionEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            // Store ResourceId in capital letters so it can be looked up later using `ListAsync` operation
            entity.ResourceId = entity.ResourceId.ToUpperInvariant();

            if (entity.FrontendIPConfiguration != null)
            {
                entity.FrontendIPConfiguration.PublicIPResourceIds = entity.FrontendIPConfiguration.PublicIPResourceIds?.Select(ip => ip.ToUpperInvariant());
                entity.FrontendIPConfiguration.PrivateIPAddresses = entity.FrontendIPConfiguration.PrivateIPAddresses?.Select(ip =>
                {
                    ip.SubnetId = ip.SubnetId.ToUpperInvariant();
                    return ip;
                });
            }

            if (entity.NetworkInterfaceConfiguration != null)
            {
                entity.NetworkInterfaceConfiguration.DelegatedSubnetResourceIds = entity.NetworkInterfaceConfiguration.DelegatedSubnetResourceIds?.Select(rid => rid.ToUpperInvariant());
            }

            if (!string.IsNullOrWhiteSpace(entity.ManagedResourceGroupName))
            {
                entity.ManagedResourceGroupName = entity.ManagedResourceGroupName.ToUpperInvariant();
            }

            var builder = Builders<VNetInjectionEntity>.Filter;
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

            return entity;
        }
    }
}
