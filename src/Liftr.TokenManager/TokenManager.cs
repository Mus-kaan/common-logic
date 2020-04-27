//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    public class TokenManager : ITokenManager
    {
        private readonly TokenManagerConfiguration _tokenManagerConfiguration;
        private readonly CertificateStore _certificateStore;
        private readonly ConcurrentDictionary<string, AuthenticationContext> _authContexts;

        public TokenManager(TokenManagerConfiguration tokenConfiguration, CertificateStore certificateStore = null)
        {
            _tokenManagerConfiguration = tokenConfiguration ?? throw new ArgumentNullException(nameof(tokenConfiguration));
            _certificateStore = certificateStore;
            _authContexts = new ConcurrentDictionary<string, AuthenticationContext>();

            if (!string.IsNullOrEmpty(_tokenManagerConfiguration.TenantId))
            {
                var defaultAuthContext = new AuthenticationContext($"{_tokenManagerConfiguration.AadEndpoint}/{_tokenManagerConfiguration.TenantId}");
                _authContexts[_tokenManagerConfiguration.TenantId] = defaultAuthContext;
            }
        }

        public async Task<string> GetTokenAsync(string clientId, string clientSecret, string tenantId = null)
        {
            var token = await GetAuthContextForTenant(tenantId)
                .AcquireTokenAsync(_tokenManagerConfiguration.TargetResource, new ClientCredential(clientId, clientSecret));

            return token.AccessToken;
        }

        public async Task<string> GetTokenAsync(Uri keyVaultEndpoint, string clientId, string certificateName, string tenantId = null, bool sendX5c = false)
        {
            if (_certificateStore == null)
            {
                throw new InvalidOperationException($"No {nameof(CertificateStore)} found to retrieve the certificate");
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            var cert = await _certificateStore.GetCertificateAsync(keyVaultEndpoint, certificateName);
#pragma warning restore CA2000 // Dispose objects before losing scope
            return await GetTokenAsync(clientId, cert, tenantId, sendX5c);
        }

        public async Task<string> GetTokenAsync(string clientId, X509Certificate2 certificate, string tenantId = null, bool sendX5c = false)
        {
            var clientAssertion = new ClientAssertionCertificate(clientId, certificate);
            var authContext = GetAuthContextForTenant(tenantId);
            var token = await authContext.AcquireTokenAsync(_tokenManagerConfiguration.TargetResource, clientAssertion, sendX5c: sendX5c);
            return token.AccessToken;
        }

        private AuthenticationContext GetAuthContextForTenant(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = _tokenManagerConfiguration.TenantId;
            }

            return _authContexts.GetOrAdd(tenantId, new AuthenticationContext(authority: $"{_tokenManagerConfiguration.AadEndpoint}/{tenantId}", validateAuthority: true));
        }
    }
}