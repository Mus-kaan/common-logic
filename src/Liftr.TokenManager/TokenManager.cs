//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    public class TokenManager : ITokenManager
    {
        private readonly TokenManagerConfiguration _tokenManagerConfiguration;
        private readonly AuthenticationContext _authContext;
        private readonly ITokenCache _cache;

        public TokenManager(IOptions<TokenManagerConfiguration> tokenConfiguration)
        {
            if (tokenConfiguration == null)
            {
                throw new ArgumentNullException(nameof(tokenConfiguration));
            }

            _tokenManagerConfiguration = tokenConfiguration.Value;
            _authContext = new AuthenticationContext($"{_tokenManagerConfiguration.AadEndpoint}/{_tokenManagerConfiguration.TenantId}");
            _cache = new TokenCache(TimeSpan.FromMinutes(10));
        }

        public async Task<string> GetTokenAsync(string clientId, string clientSecret)
        {
            var token = _cache.GetTokenItem(clientId);
            if (token == null)
            {
                token = await _authContext
                            .AcquireTokenAsync(
                                _tokenManagerConfiguration.ArmEndpoint,
                                new ClientCredential(clientId, clientSecret));
                _cache.SetTokenItem(clientId, token);
            }

            return token.AccessToken;
        }

        public async Task<string> GetTokenAsync(string clientId, X509Certificate2 certificate)
        {
            var token = _cache.GetTokenItem(clientId);

            if (token == null)
            {
                var clientAssertion = new ClientAssertionCertificate(clientId, certificate);
                token = await _authContext.AcquireTokenAsync(_tokenManagerConfiguration.ArmEndpoint, clientAssertion);
                _cache.SetTokenItem(clientId, token);
            }

            return token.AccessToken;
        }
    }
}
