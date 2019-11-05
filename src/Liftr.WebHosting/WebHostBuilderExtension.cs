//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Liftr.Configuration;
using System;
using System.Net.Http;

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
                    var tokenProvider = new AzureServiceTokenProvider();
                    var callback = new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback);
                    kvClient = new KeyVaultClient(callback);
                }
                else
                {
                    string clientId = context.Configuration["ClientId"] ??
                        throw new InvalidOperationException("Please provide AAD ClientId using 'ClientId'");
                    string clientSecret = context.Configuration["ClientSecret"] ??
                        throw new InvalidOperationException("Please provide AAD ClientSecret using 'ClientSecret'");
                    kvClient = new KeyVaultClient(
                        new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) =>
                        {
                            var authContext = new AuthenticationContext(authority, TokenCache.DefaultShared);
                            var result = await authContext.AcquireTokenAsync(resource, new ClientCredential(clientId, clientSecret));
                            return result.AccessToken;
#pragma warning disable CA2000 // Dispose objects before losing scope
                        }), new HttpClient());
#pragma warning restore CA2000 // Dispose objects before losing scope
                }

                services.AddSingleton<IKeyVaultClient, KeyVaultClient>((sp) => kvClient);
            });
        }

        /// <summary>
        /// This will load all the secrets start with 'secretsPrefix', the prefix will be removed when load in memory. Sample secret name: "prefix-Logging--LogLevel--Default".
        /// </summary>
        /// <param name="builder">web host builder</param>
        /// <param name="secretsPrefix">The prefix filter value</param>
        /// <param name="useManagedIdentity">use managed identity to authenticate with key vault</param>
        /// <returns></returns>
        public static IWebHostBuilder UseKeyVaultProvider(this IWebHostBuilder builder, string secretsPrefix, bool useManagedIdentity = true)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddKeyVaultConfigurations(secretsPrefix, useManagedIdentity);
            });
        }
    }
}
