//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Liftr.Configuration;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.KeyVault;
using System;

namespace Microsoft.Liftr.WebHosting
{
    public static class WebHostBuilderExtension
    {
        /// <summary>
        /// 1. This will load all the secrets start with 'secretsPrefix', the prefix will be removed when load in memory. Sample secret name: "prefix-Logging--LogLevel--Default".
        /// 2. Add the TokenCredential for the identity that used to load the secrets.
        /// 3. Add a Key Vault client of that identity.
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

            builder = builder.ConfigureAppConfiguration((context, config) =>
              {
                  config.AddKeyVaultConfigurations(secretsPrefix);
              });

            return builder.AddCredentials();
        }

        /// <summary>
        /// 1. Add the TokenCredential for the identity that used to load the secrets.
        /// 2. Add a Key Vault client of that identity.
        /// </summary>
        /// <param name="builder">web host builder</param>
        /// <returns></returns>
        public static IWebHostBuilder AddCredentials(this IWebHostBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureServices((context, services) =>
            {
                TokenCredential tokenCredential = null;
                KeyVaultClient kvClient = null;

                string clientId = context.Configuration[GlobalSettingConstants.ClientId];
                string tenantId = context.Configuration["TenantId"];
                string clientSecret = context.Configuration[GlobalSettingConstants.ClientSecret];

                if (string.IsNullOrEmpty(clientId) ||
                string.IsNullOrEmpty(tenantId) ||
                string.IsNullOrEmpty(clientSecret))
                {
                    Console.WriteLine("Using Managed Identity to initialized key vault client and tokenCredential. Then add them to dependency injection container.");
                    kvClient = KeyVaultClientFactory.FromMSI();

                    // Enabling retries here because there is a chance that the IDMS endpoint
                    // will not be available by the time the application is starting up.
                    var tokenCredentialOptions = new TokenCredentialOptions();
                    tokenCredentialOptions.Retry.Delay = TimeSpan.FromSeconds(5);
                    tokenCredentialOptions.Retry.MaxRetries = 12;
                    tokenCredential = new ManagedIdentityCredential(options: tokenCredentialOptions);
                }
                else
                {
                    Console.WriteLine($"Using client Id '{clientId}' and client secret to initialized key vault client and tokenCredential. Then add them to dependency injection container.");
                    kvClient = KeyVaultClientFactory.FromClientIdAndSecret(clientId, clientSecret);
                    tokenCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                }

                services.AddSingleton<IKeyVaultClient, KeyVaultClient>((sp) => kvClient);
                services.AddSingleton(tokenCredential);
            });
        }
    }
}
