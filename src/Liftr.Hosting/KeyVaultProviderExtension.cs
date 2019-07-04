//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Liftr.Hosting
{
    public static class KeyVaultProviderExtension
    {
        public static IWebHostBuilder UseKeyVaultProvider(this IWebHostBuilder builder, string secretsPrefix)
        {
            return builder.ConfigureAppConfiguration((context, config) =>
            {
                var builtConfig = config.Build();

                string vaultEndpoint = builtConfig["VaultEndpoint"];
                if (!string.IsNullOrEmpty(vaultEndpoint))
                {
                    string clientId = builtConfig["ClientId"] ?? throw new InvalidOperationException("Please provide AAD ClientId using 'ClientId'");
                    string clientSecret = builtConfig["ClientSecret"] ?? throw new InvalidOperationException("Please provide AAD ClientSecret using 'ClientSecret'");
                    config.AddAzureKeyVault(vaultEndpoint, clientId, clientSecret, new PrefixKeyVaultSecretManager(secretsPrefix));
                }
            });
        }
    }
}
