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

            builder = builder.ConfigureAppConfiguration((context, config) =>
              {
                  config.AddKeyVaultConfigurations(secretsPrefix);
              });

            return builder.ConfigureServices((context, services) =>
            {
                KeyVaultClient kvClient = null;

                string clientId = context.Configuration["ClientId"];
                string clientSecret = context.Configuration["ClientSecret"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    kvClient = KeyVaultClientFactory.FromMSI();
                }
                else
                {
                    kvClient = KeyVaultClientFactory.FromClientIdAndSecret(clientId, clientSecret);
                }

                services.AddSingleton<IKeyVaultClient, KeyVaultClient>((sp) => kvClient);
            });
        }
    }
}
