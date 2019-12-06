//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Configuration;
using Microsoft.Liftr.KeyVault;
using System;

namespace Microsoft.Liftr.GenericHosting
{
    public static class GenericHostBuilderExtension
    {
        /// <summary>
        /// This will add the configuration from the following sources:
        /// 1. appsettings.json
        /// 2. appsettings.[EnvName].json
        /// 3. Environment variables
        /// </summary>
        /// <param name="builder">generic host builder</param>
        /// <param name="environmentVariablePrefix">The prefix of the environment variables. This will be removed when load to application.</param>
        /// <returns></returns>
        public static IHostBuilder UseDefaultAppConfig(this IHostBuilder builder, string environmentVariablePrefix = null)
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
                config.AddJsonFile("appsettings.json", optional: false);
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

        /// <summary>
        /// This will add the configuration from the following sources:
        /// 1. appsettings.json
        /// 2. appsettings.[EnvName].json
        /// 3. Key vault secrets.
        /// 4. Environment variables
        /// </summary>
        /// <param name="builder">generic host builder</param>
        /// <param name="keyVaultPrefix">The prefix of the key vault secrets. This will be removed when load to application.</param>
        /// <param name="environmentVariablePrefix">The prefix of the environment variables. This will be removed when load to application.</param>
        public static IHostBuilder UseDefaultAppConfigWithKeyVault(this IHostBuilder builder, string keyVaultPrefix, string environmentVariablePrefix = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Work around the following issue which will be fixed in .Net Core 3.0.
            // https://github.com/aspnet/AspNetCore/issues/4150
            builder.UseEnvironment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");

            builder = builder.ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Environment.CurrentDirectory);
                config.AddJsonFile("appsettings.json", optional: false);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);

                config.AddKeyVaultConfigurations(keyVaultPrefix);

                if (string.IsNullOrEmpty(environmentVariablePrefix))
                {
                    config.AddEnvironmentVariables();
                }
                else
                {
                    config.AddEnvironmentVariables(prefix: environmentVariablePrefix);
                }
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

        /// <summary>
        /// This will load all the secrets start with 'secretsPrefix'. Sample secret name: "prefix-Logging--LogLevel--Default".
        /// </summary>
        public static IHostBuilder UseManagedIdentityAndKeyVault(this IHostBuilder builder, string secretsPrefix)
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
