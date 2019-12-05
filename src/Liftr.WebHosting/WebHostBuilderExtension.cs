//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Liftr.Configuration;
using Microsoft.Liftr.KeyVault;
using System;

namespace Microsoft.Liftr.WebHosting
{
    public static class WebHostBuilderExtension
    {
        /// <summary>
        /// This will add a singleton for injecting an instance of KeyVaultClient for the IKeyVaultClient interface.
        /// </summary>
        /// <param name="builder">web host builder</param>
        /// <param name="useManagedIdentity">use managed identity to authenticate with key vault</param>
        /// <returns>web host builder with the singleton configured</returns>
        public static IWebHostBuilder UseKeyVaultClient(this IWebHostBuilder builder, bool useManagedIdentity = true)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureServices((context, services) =>
            {
                KeyVaultClient kvClient = null;
                if (useManagedIdentity)
                {
                    kvClient = KeyVaultClientFactory.FromMSI();
                }
                else
                {
                    string clientId = context.Configuration["ClientId"] ??
                        throw new InvalidOperationException("Please provide AAD ClientId using 'ClientId'");

                    string clientSecret = context.Configuration["ClientSecret"] ??
                        throw new InvalidOperationException("Please provide AAD ClientSecret using 'ClientSecret'");

                    kvClient = KeyVaultClientFactory.FromClientIdAndSecret(clientId, clientSecret);
                }

                services.AddSingleton<IKeyVaultClient, KeyVaultClient>((sp) => kvClient);
            });
        }

        /// <summary>
        /// This will load all the secrets start with 'secretsPrefix', the prefix will be removed when load in memory. Sample secret name: "prefix-Logging--LogLevel--Default".
        /// </summary>
        /// <param name="builder">web host builder</param>
        /// <param name="secretsPrefix">The prefix filter value</param>
        /// <returns></returns>
        public static IWebHostBuilder UseKeyVaultProvider(this IWebHostBuilder builder, string secretsPrefix)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddKeyVaultConfigurations(secretsPrefix);
            });
        }
    }
}
