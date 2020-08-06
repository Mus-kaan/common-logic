//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Liftr.Utilities;
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

            string vaultEndpoint = builtConfig[GlobalSettingConstants.VaultEndpoint];

            if (string.IsNullOrEmpty(vaultEndpoint))
            {
                var meta = InstanceMetaHelper.GetMetaInfoAsync().Result;
                vaultEndpoint = meta?.GetComputeTagMetadata()?.VaultEndpoint;

                if (!string.IsNullOrEmpty(vaultEndpoint))
                {
                    Console.WriteLine($"Loaded key vault endpoint '{vaultEndpoint}' from compute tag.");
                }
            }

            if (!string.IsNullOrEmpty(vaultEndpoint))
            {
                string clientId = builtConfig[GlobalSettingConstants.ClientId];
                string clientSecret = builtConfig[GlobalSettingConstants.ClientSecret];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    Console.WriteLine($"Using Managed Identity to load key vault '{vaultEndpoint}' secret into configuration.");
                    var kvClient = KeyVaultClientFactory.FromMSI();
                    config.AddAzureKeyVault(vaultEndpoint, kvClient, new PrefixKeyVaultSecretManager(secretsPrefix));
                }
                else
                {
                    Console.WriteLine($"Using client Id '{clientId}' and client secret to to load key vault '{vaultEndpoint}' secret into configuration.");
                    config.AddAzureKeyVault(vaultEndpoint, clientId, clientSecret, new PrefixKeyVaultSecretManager(secretsPrefix));
                }
            }
        }
    }
}
