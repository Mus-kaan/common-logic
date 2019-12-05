//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Liftr.KeyVault;
using System;

namespace Microsoft.Liftr.Configuration
{
    public static class ConfigurationBuilderExtension
    {
        /// <summary>
        /// This will load all the secrets start with 'secretsPrefix', the prefix will be removed when load in memory. Sample secret name: "prefix-Logging--LogLevel--Default".
        /// </summary>
        public static void AddKeyVaultConfigurations(this IConfigurationBuilder config, string secretsPrefix)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var builtConfig = config.Build();

            string vaultEndpoint = builtConfig["VaultEndpoint"];
            if (!string.IsNullOrEmpty(vaultEndpoint))
            {
                Console.WriteLine($"Start loading secrets from vault '{vaultEndpoint}' into configuration.");

                string clientId = builtConfig["ClientId"];
                string clientSecret = builtConfig["ClientSecret"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    Console.WriteLine("Using MSI to authenticate against KeyVault.");
                    var kvClient = KeyVaultClientFactory.FromMSI();
                    config.AddAzureKeyVault(vaultEndpoint, kvClient, new PrefixKeyVaultSecretManager(secretsPrefix));
                }
                else
                {
                    Console.WriteLine("Using client Id and client secret to authenticate against KeyVault. ClientId: " + clientId);
                    config.AddAzureKeyVault(vaultEndpoint, clientId, clientSecret, new PrefixKeyVaultSecretManager(secretsPrefix));
                }
            }
        }
    }
}
