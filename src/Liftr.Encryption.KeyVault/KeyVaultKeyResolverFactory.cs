//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Encryption
{
    public static class KeyVaultKeyResolverFactory
    {
        public static KeyVaultKeyResolver FromClientIdAndSecret(string clientId, string clientSecret)
        {
            return new KeyVaultKeyResolver(
                async (authority, resource, scope) =>
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
