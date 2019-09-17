//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Liftr.Configuration
{
    public static class ConfigurationBuilderExtension
    {
        /// <summary>
        /// This will load all the secrets start with 'secretsPrefix', the prefix will be removed when load in memory. Sample secret name: "prefix-Logging--LogLevel--Default".
        /// </summary>
        public static void AddKeyVaultConfigurations(this IConfigurationBuilder config, string secretsPrefix, bool useManagedIdentity = true)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var builtConfig = config.Build();

            string vaultEndpoint = builtConfig["VaultEndpoint"];
            if (!string.IsNullOrEmpty(vaultEndpoint))
            {
                if (useManagedIdentity)
                {
                    // https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity#obtaining-tokens-for-azure-resources
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
#pragma warning restore CA2000 // Dispose objects before losing scope
                    config.AddAzureKeyVault(vaultEndpoint, kv, new PrefixKeyVaultSecretManager(secretsPrefix));
                }
                else
                {
                    string clientId = builtConfig["ClientId"] ?? throw new InvalidOperationException("Please provide AAD ClientId using 'ClientId'");
                    string clientSecret = builtConfig["ClientSecret"] ?? throw new InvalidOperationException("Please provide AAD ClientSecret using 'ClientSecret'");
                    config.AddAzureKeyVault(vaultEndpoint, clientId, clientSecret, new PrefixKeyVaultSecretManager(secretsPrefix));
                }
            }
        }
    }
}
