//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Redis.Fluent;
using Microsoft.Azure.Management.Redis.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal partial class LiftrAzure
    {
        #region Redis Cache
        public async Task<IRedisCache> GetOrCreateRedisCacheAsync(
            Region location,
            string rgName,
            string redisCacheName,
            IDictionary<string, string> tags,
            IDictionary<string, string> redisConfig = null,
            CancellationToken cancellationToken = default)
        {
            var rc = await GetRedisCachesAsync(rgName, redisCacheName, cancellationToken);

            if (rc == null)
            {
                rc = await CreateRedisCacheAsync(location, rgName, redisCacheName, tags, redisConfig, cancellationToken);
            }

            return rc;
        }

        public async Task<IRedisCache> GetRedisCachesAsync(string rgName, string redisCacheName, CancellationToken cancellationToken = default)
        {
            _logger.Information($"Getting Redis Cache. rgName: {rgName}, redisCacheName: {redisCacheName} ...");

            var redisCache = await FluentClient
                .RedisCaches
                .GetByResourceGroupAsync(rgName, redisCacheName, cancellationToken);

            if (redisCache == null)
            {
                _logger.Information($"Cannot find Redis Cache. rgName: {rgName}, redisCacheName: {redisCacheName} ...");
            }

            return redisCache;
        }

        public async Task<IEnumerable<IRedisCache>> ListRedisCacheAsync(string rgName, CancellationToken cancellationToken = default)
        {
            _logger.Information($"Listing Redis Caches in rg {rgName}...");
            return await FluentClient.RedisCaches
                .ListByResourceGroupAsync(rgName, loadAllPages: true, cancellationToken: cancellationToken);
        }

        public async Task<IRedisCache> CreateRedisCacheAsync(
            Region location,
            string rgName,
            string redisCacheName,
            IDictionary<string, string> tags,
            IDictionary<string, string> redisConfig = null,
            CancellationToken cancellationToken = default)
        {
            _logger.Information($"Creating a RedisCache with name {redisCacheName} ...");

            var creatable = FluentClient.RedisCaches
            .Define(redisCacheName)
            .WithRegion(location)
            .WithExistingResourceGroup(rgName)
            .WithStandardSku(1)
            .WithMinimumTlsVersion(TlsVersion.OneFullStopTwo)
            .WithTags(tags);

            if (redisConfig != null)
            {
                creatable = creatable.WithRedisConfiguration(redisConfig);
            }

            IRedisCache redisCache = await creatable.CreateAsync(cancellationToken);
            _logger.Information($"Created RedisCache with resourceId {redisCache.Id}");

            return redisCache;
        }
        #endregion
    }
}
