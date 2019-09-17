//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Configuration;
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
        /// <param name="useManagedIdentity"></param>
        /// <returns></returns>
        public static IHostBuilder UseDefaultAppConfigWithKeyVault(this IHostBuilder builder, string keyVaultPrefix, string environmentVariablePrefix = null, bool useManagedIdentity = true)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Environment.CurrentDirectory);
                config.AddJsonFile("appsettings.json", optional: false);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);

                config.AddKeyVaultConfigurations(keyVaultPrefix, useManagedIdentity: useManagedIdentity);

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
        /// This will load all the secrets start with 'secretsPrefix'. Sample secret name: "prefix-Logging--LogLevel--Default".
        /// </summary>
        public static IHostBuilder UseManagedIdentityAndKeyVault(this IHostBuilder builder, string secretsPrefix, bool useManagedIdentity = true)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddKeyVaultConfigurations(secretsPrefix, useManagedIdentity: useManagedIdentity);
            });
        }
    }
}
