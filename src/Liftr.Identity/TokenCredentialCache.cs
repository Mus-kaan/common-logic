//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Identity
{
    public class TokenCacheItem
    {
        public DateTimeOffset RenewAfter { get; set; }

        public AccessToken Token { get; set; }
    }

    public sealed class TokenCredentialCache : TokenCredential, IDisposable
    {
        private readonly TokenCredential _tokenCredential;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, TokenCacheItem> _cache = new Dictionary<string, TokenCacheItem>();

        public TokenCredentialCache(TokenCredential tokenCredential)
        {
            _tokenCredential = tokenCredential;
        }

        public void Dispose()
        {
            _mutex.Dispose();
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var cacheKey = GetCacheKey(requestContext);

            _mutex.Wait(cancellationToken);
            try
            {
                if (TryGetCachedToken(cacheKey, out var cache))
                {
                    return cache.Token;
                }

                var newToken = _tokenCredential.GetToken(requestContext, cancellationToken);
                UpdateCache(cacheKey, newToken);
                return newToken;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var cacheKey = GetCacheKey(requestContext);

            await _mutex.WaitAsync(cancellationToken);
            try
            {
                if (TryGetCachedToken(cacheKey, out var cache))
                {
                    return cache.Token;
                }

                var newToken = await _tokenCredential.GetTokenAsync(requestContext, cancellationToken);
                UpdateCache(cacheKey, newToken);
                return newToken;
            }
            finally
            {
                _mutex.Release();
            }
        }

        private bool TryGetCachedToken(string cacheKey, out TokenCacheItem cache)
        {
            cache = null;
            if (_cache.ContainsKey(cacheKey))
            {
                cache = _cache[cacheKey];
                if (DateTimeOffset.UtcNow < cache.RenewAfter)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateCache(string cacheKey, AccessToken token)
        {
            var ttlMs = (token.ExpiresOn - DateTimeOffset.UtcNow).TotalMilliseconds * 0.4;
            var renewAfter = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(ttlMs);
            if (renewAfter > token.ExpiresOn)
            {
                renewAfter = token.ExpiresOn;
            }

            _cache[cacheKey] = new TokenCacheItem() { Token = token, RenewAfter = renewAfter };
        }

        private static string GetCacheKey(TokenRequestContext requestContext)
        {
            return string.Join("-", requestContext.Scopes);
        }
    }
}
