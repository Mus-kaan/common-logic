//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    public class TokenManager : ITokenManager
    {
        private readonly TokenManagerConfiguration _tokenManagerConfiguration;
        private readonly ConcurrentDictionary<string, AuthenticationContext> _authContexts;

        public TokenManager(TokenManagerConfiguration tokenConfiguration)
        {
            if (tokenConfiguration == null)
            {
                throw new ArgumentNullException(nameof(tokenConfiguration));
            }

            _tokenManagerConfiguration = tokenConfiguration;
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

        public async Task<string> GetTokenAsync(string clientId, X509Certificate2 certificate, string tenantId = null)
        {
            var clientAssertion = new ClientAssertionCertificate(clientId, certificate);
            var token = await GetAuthContextForTenant(tenantId)
                .AcquireTokenAsync(_tokenManagerConfiguration.TargetResource, clientAssertion);

            return token.AccessToken;
        }

        private AuthenticationContext GetAuthContextForTenant(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = _tokenManagerConfiguration.TenantId;
            }

            AuthenticationContext targetAuthContext;

            if (!_authContexts.ContainsKey(tenantId))
            {
                targetAuthContext = new AuthenticationContext($"{_tokenManagerConfiguration.AadEndpoint}/{tenantId}");
                _authContexts[tenantId] = targetAuthContext;
            }
            else
            {
                targetAuthContext = _authContexts[tenantId];
            }

            return targetAuthContext;
        }
    }
}
