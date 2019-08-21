//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Liftr.Hosting
{
    public static class WebHostBuilderExtension
    {
        /// <summary>
        /// This will load all the secrets start with 'secretsPrefix'. Sample secret name: "prefix-Logging--LogLevel--Default".
        /// </summary>
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

        /// <summary>
        /// This will load all the secrets start with 'secretsPrefix'. Sample secret name: "prefix-Logging--LogLevel--Default".
        /// </summary>
        public static IWebHostBuilder UseManagedIdentityAndKeyVault(this IWebHostBuilder builder, string secretsPrefix)
        {
            return builder.ConfigureAppConfiguration((context, config) =>
            {
                var builtConfig = config.Build();

                string vaultEndpoint = builtConfig["VaultEndpoint"];
                if (!string.IsNullOrEmpty(vaultEndpoint))
                {
                    // https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity#obtaining-tokens-for-azure-resources
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                    config.AddAzureKeyVault(vaultEndpoint, kv, new PrefixKeyVaultSecretManager(secretsPrefix));
                }
            });
        }
    }
}
