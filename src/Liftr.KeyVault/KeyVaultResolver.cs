//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.KeyVault
{
    public class KeyVaultResolver
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly KeyVaultKeyResolver _cloudResolver;

        public KeyVaultResolver(string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            _clientId = clientId;

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new ArgumentNullException(nameof(clientSecret));
            }

            _clientSecret = clientSecret;

            _cloudResolver = new KeyVaultKeyResolver(GetTokenAsync);
        }

        public async Task<SymmetricKey> ResolveSecretAsSymmetricKeyAsync(string keyIdentifier, CancellationToken cancellationToken)
        {
            // Requirments about secrets to be parsed as symetricKey:
            // https://docs.microsoft.com/en-us/azure/storage/blobs/storage-encrypt-decrypt-blobs-key-vault#use-key-vault-secrets
            if (string.IsNullOrWhiteSpace(keyIdentifier) || !SecretIdentifier.IsSecretIdentifier(keyIdentifier))
            {
                throw new ArgumentNullException(nameof(keyIdentifier));
            }

            var symmetricKey = (SymmetricKey)await _cloudResolver.ResolveKeyAsync(keyIdentifier, cancellationToken);

            if (symmetricKey == null)
            {
                throw new InvalidOperationException("Secret is not a valid symmetricKey");
            }

            return symmetricKey;
        }

        private async Task<string> GetTokenAsync(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(_clientId, _clientSecret);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }
    }
}
