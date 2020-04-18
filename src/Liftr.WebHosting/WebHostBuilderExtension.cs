﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
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

            return builder.ConfigureServices((context, services) =>
            {
                TokenCredential tokenCredential = null;
                KeyVaultClient kvClient = null;

                string clientId = context.Configuration["ClientId"];
                string tenantId = context.Configuration["TenantId"];
                string clientSecret = context.Configuration["ClientSecret"];

                if (string.IsNullOrEmpty(clientId) ||
                string.IsNullOrEmpty(tenantId) ||
                string.IsNullOrEmpty(clientSecret))
                {
                    kvClient = KeyVaultClientFactory.FromMSI();
                    tokenCredential = new ManagedIdentityCredential();
                }
                else
                {
                    kvClient = KeyVaultClientFactory.FromClientIdAndSecret(clientId, clientSecret);
                    tokenCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                }

                services.AddSingleton<IKeyVaultClient, KeyVaultClient>((sp) => kvClient);
                services.AddSingleton(tokenCredential);
            });
        }
    }
}
