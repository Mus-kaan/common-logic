//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;

namespace Microsoft.Liftr.KeyVault
{
    public static class KeyVaultClientFactory
    {
        public static KeyVaultClient FromClientIdAndSecret(string clientId, string clientSecret)
        {
            return new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) =>
                {
                    var authContext = new AuthenticationContext(authority, TokenCache.DefaultShared);
                    var result = await authContext.AcquireTokenAsync(resource, new ClientCredential(clientId, clientSecret));
                    return result.AccessToken;
#pragma warning disable CA2000 // Dispose objects before losing scope
                }), new HttpClient());
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        public static KeyVaultClient FromMSI()
        {
            var tokenProvider = new AzureServiceTokenProvider();
            var callback = new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback);
            return new KeyVaultClient(callback);
        }
    }
}
