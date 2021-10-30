//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Identity.Client;
using System;
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
                    var app = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri(authority))
                    .WithLegacyCacheCompatibility(false)
                    .Build();

                    var tokenScope = $"{resource}/.default";

                    var result = await app
                    .AcquireTokenForClient(new string[] { tokenScope })
                    .ExecuteAsync();

                    return result.AccessToken;
#pragma warning disable CA2000 // Dispose objects before losing scope
                }), new HttpClient());
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        public static KeyVaultClient FromClientIdAndCertificate(string clientId, X509Certificate2 certificate)
        {
            return new KeyVaultClient(async (authority, resource, scope) =>
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithCertificate(certificate)
                    .WithAuthority(new Uri(authority))
                    .WithLegacyCacheCompatibility(false)
                    .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                    .Build();

                var tokenScope = $"{resource}/.default";

                var result = await app
                .AcquireTokenForClient(new string[] { tokenScope })
                .ExecuteAsync();

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
