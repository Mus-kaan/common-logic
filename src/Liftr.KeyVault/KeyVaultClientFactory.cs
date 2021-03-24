//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Liftr.KeyVault
{
    public static class KeyVaultClientFactory
    {
        public static KeyVaultClient FromClientIdAndSecret(string clientId, string clientSecret)
        {
            return new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) =>
                {
                    var authContext = new AuthenticationContext(authority);
                    var result = await authContext.AcquireTokenAsync(resource, new ClientCredential(clientId, clientSecret));
                    return result.AccessToken;
#pragma warning disable CA2000 // Dispose objects before losing scope
                }), new HttpClient());
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        public static KeyVaultClient FromClientIdAndCertificate(string clientId, X509Certificate2 certificate, string aadEndPoint, string tenantId)
        {
            return new KeyVaultClient(async (authority, resource, scope) =>
            {
                var authenticationContext = new AuthenticationContext(authority: $"{aadEndPoint}/{tenantId}", validateAuthority: true);
                var clientAssertionCertificate = new ClientAssertionCertificate(clientId, certificate);
                var result = await authenticationContext.AcquireTokenAsync(resource, clientAssertionCertificate, sendX5c: true);
                return result.AccessToken;
            });
        }

        public static KeyVaultClient FromMSI()
        {
            var tokenProvider = new AzureServiceTokenProvider();
            var callback = new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback);
            return new KeyVaultClient(callback);
        }
    }
}
