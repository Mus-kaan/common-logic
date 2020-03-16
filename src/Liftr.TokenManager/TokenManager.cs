//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

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

        public TokenManager(TokenManagerConfiguration tokenConfiguration)
        {
            if (tokenConfiguration == null)
            {
                throw new ArgumentNullException(nameof(tokenConfiguration));
            }

            _tokenManagerConfiguration = tokenConfiguration;
            _authContext = new AuthenticationContext($"{_tokenManagerConfiguration.AadEndpoint}/{_tokenManagerConfiguration.TenantId}");
        }

        public async Task<string> GetTokenAsync(string clientId, string clientSecret)
        {
            var token = await _authContext.AcquireTokenAsync(_tokenManagerConfiguration.TargetResource, new ClientCredential(clientId, clientSecret));

            return token.AccessToken;
        }

        public async Task<string> GetTokenAsync(string clientId, X509Certificate2 certificate)
        {
            var clientAssertion = new ClientAssertionCertificate(clientId, certificate);
            var token = await _authContext.AcquireTokenAsync(_tokenManagerConfiguration.TargetResource, clientAssertion);

            return token.AccessToken;
        }
    }
}
