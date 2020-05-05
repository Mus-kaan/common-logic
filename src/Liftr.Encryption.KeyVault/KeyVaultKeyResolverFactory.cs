//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Liftr.Encryption
{
    public static class KeyVaultKeyResolverFactory
    {
        public static KeyVaultKeyResolver FromClientIdAndSecret(string clientId, string clientSecret)
        {
            return new KeyVaultKeyResolver(
                async (authority, resource, scope) =>
                {
                    var authContext = new AuthenticationContext(authority);
                    var result = await authContext.AcquireTokenAsync(resource, new ClientCredential(clientId, clientSecret));
                    return result.AccessToken;
                });
        }

        public static KeyVaultKeyResolver FromMSI()
        {
            var tokenProvider = new AzureServiceTokenProvider();
            var callback = new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback);
            return new KeyVaultKeyResolver(callback);
        }
    }
}
