//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Configuration;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.KeyVault;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Liftr.ImageBuilder")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.SimpleDeploy")]

namespace Microsoft.Liftr.GenericHosting
{
    public static class GenericHostBuilderExtension
    {
        /// <summary>
        /// 1. This will load all the secrets start with 'secretsPrefix', the prefix will be removed when load in memory. Sample secret name: "prefix-Logging--LogLevel--Default".
        /// 2. Add the TokenCredential for the identity that used to load the secrets.
        /// 3. Add a Key Vault client of that identity.
        /// </summary>
        /// <param name="builder">generic host builder</param>
        /// <param name="keyVaultPrefix">The prefix of the key vault secrets. This will be removed when load to application.</param>
        public static IHostBuilder UseKeyVaultProvider(this IHostBuilder builder, string keyVaultPrefix)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder = builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddKeyVaultConfigurations(keyVaultPrefix);
            });

            return builder.AddCredentials();
        }

        /// <summary>
        /// 1. Add the TokenCredential for the identity that used to load the secrets.
        /// 2. Add a Key Vault client of that identity.
        /// </summary>
        /// <param name="builder">generic host builder</param>
        public static IHostBuilder AddCredentials(this IHostBuilder builder)
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
                    tokenCredential = new ManagedIdentityCredential();
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

        /// <summary>
        /// This will add the configuration from the following sources:
        /// 1. appsettings.json
        /// 2. appsettings.[EnvName].json
        /// 3. Environment variables
        /// </summary>
        /// <param name="builder">generic host builder</param>
        /// <param name="environmentVariablePrefix">The prefix of the environment variables. This will be removed when load to application.</param>
        /// <returns></returns>
        internal static IHostBuilder UseDefaultAppConfig(this IHostBuilder builder, string environmentVariablePrefix = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Work around the following issue which will be fixed in .Net Core 3.0.
            // https://github.com/aspnet/AspNetCore/issues/4150
            builder.UseEnvironment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");

            return builder.ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Environment.CurrentDirectory);
                config.AddJsonFile("embedded-appsettings.json", optional: true);
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                if (string.IsNullOrEmpty(environmentVariablePrefix))
                {
                    config.AddEnvironmentVariables();
                }
                else
                {
                    config.AddEnvironmentVariables(prefix: environmentVariablePrefix);
                }
            });
        }
    }
}
