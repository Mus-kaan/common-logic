//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Client;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    public sealed class TokenManager : ITokenManager, IDisposable
    {
        private const string c_msalAppCachePrefix = "token_manager_msal_app";

        private readonly TokenManagerConfiguration _tokenManagerConfiguration;
        private readonly CertificateStore _certificateStore;
        private readonly IMemoryCache _memoryCache;
        private readonly bool _disposeMemoryCache;
        private readonly string[] _tokenScopes;

        public TokenManager(TokenManagerConfiguration tokenConfiguration, CertificateStore certificateStore = null, IMemoryCache memoryCache = null)
        {
            _tokenManagerConfiguration = tokenConfiguration ?? throw new ArgumentNullException(nameof(tokenConfiguration));

            // Ensure AAD endpoint doesn't have any tailing whitespace or forward slash as it will be used to construct authority URL with tenant ID.
            _tokenManagerConfiguration.AadEndpoint = _tokenManagerConfiguration.AadEndpoint.TrimEnd().TrimEnd('/');

            _certificateStore = certificateStore;
            if (memoryCache == null)
            {
                memoryCache = new MemoryCache(new MemoryCacheOptions());
                _disposeMemoryCache = true;
            }

            _memoryCache = memoryCache;

            // ARM Token scopes
            // IMPORTANT: It needs to end with //.default (Note the double slash.)
            // see https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-daemon-acquire-token?tabs=dotnet#azure-ad-v10-resources
            _tokenScopes = new string[] { $"{_tokenManagerConfiguration.TargetResource}/.default" };
        }

        public void Dispose()
        {
            if (_disposeMemoryCache)
            {
                _memoryCache.Dispose();
            }
        }

        public async Task<string> GetTokenAsync(
            string clientId,
            string clientSecret,
            string tenantId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = _tokenManagerConfiguration.TenantId;
            }

            var msalApp = GetMSALApp(tenantId, clientId, clientSecret);

            var authResult = await msalApp
               .AcquireTokenForClient(_tokenScopes)
               .ExecuteAsync(cancellationToken);

            return authResult.AccessToken;
        }

        public async Task<string> GetTokenAsync(
            Uri keyVaultEndpoint,
            string clientId,
            string certificateName,
            string tenantId = null,
            bool sendX5c = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = _tokenManagerConfiguration.TenantId;
            }

            var msalApp = await GetMSALAppAsync(tenantId, clientId, keyVaultEndpoint, certificateName, cancellationToken);

            var authResult = await msalApp
                .AcquireTokenForClient(_tokenScopes)
                .WithSendX5C(sendX5c)
                .ExecuteAsync(cancellationToken);

            return authResult.AccessToken;
        }

        private async Task<IConfidentialClientApplication> GetMSALAppAsync(
           string tenantId,
           string clientId,
           Uri keyVaultEndpoint,
           string certificateName,
           CancellationToken cancellationToken)
        {
            if (_certificateStore == null)
            {
                throw new InvalidOperationException($"No {nameof(CertificateStore)} found to retrieve the certificate");
            }

            // MSAL has it's own in memory cach.
            // https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-acquire-cache-tokens
            string cacheKey = $"{c_msalAppCachePrefix}_{tenantId}_{clientId}_cert_{certificateName}";
            cacheKey = cacheKey.ToLowerInvariant();

            IConfidentialClientApplication confidentialClientApplication = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_certificateStore.CertificateCacheTTL);

                entry.SetOptions(cacheEntryOptions);

                X509Certificate2 cert = await _certificateStore.GetCertificateAsync(keyVaultEndpoint, certificateName, cancellationToken);

                return ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithCertificate(cert)
                    .WithAuthority(new Uri($"{_tokenManagerConfiguration.AadEndpoint}/{tenantId}"))
                    .WithLegacyCacheCompatibility(false)
                    .Build();
            });

            return confidentialClientApplication;
        }

        private IConfidentialClientApplication GetMSALApp(
           string tenantId,
           string clientId,
           string clientSecret)
        {
            // MSAL has it's own in memory cach.
            // https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-acquire-cache-tokens
            string cacheKey = $"{c_msalAppCachePrefix}_{tenantId}_{clientId}_secret";
            cacheKey = cacheKey.ToLowerInvariant();

            IConfidentialClientApplication confidentialClientApplication = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                return ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri($"{_tokenManagerConfiguration.AadEndpoint}/{tenantId}"))
                    .WithLegacyCacheCompatibility(false)
                    .Build();
            });

            return confidentialClientApplication;
        }
    }
}