//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.TokenManager;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.RPaaS.Hosting
{
    public static class StartupExtensions
    {
        public static void AddMetaRPClient(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddSingleton<IMetaRPStorageClient, MetaRPStorageClient>((sp) =>
            {
                var logger = sp.GetService<Serilog.ILogger>();

                var metaRPOptions = sp.GetService<IOptions<MetaRPOptions>>().Value;

                if (metaRPOptions == null
                || string.IsNullOrEmpty(metaRPOptions.MetaRPEndpoint)
                || string.IsNullOrEmpty(metaRPOptions.AccessorCertificateName))
                {
                    var ex = new InvalidOperationException($"[RPaaS Init] Please make sure '{nameof(MetaRPOptions)}' is set in the configuration.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var kvClient = sp.GetService<IKeyVaultClient>();
                if (kvClient == null)
                {
                    var ex = new InvalidOperationException("[RPaaS Init] Cannot find a key vault client in the dependency injection container to initizlize RPaaS client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var tokenManagerConfiguration = sp.GetService<IOptions<MetaRPOptions>>().Value.TokenManagerConfiguration;
                if (tokenManagerConfiguration == null
                || string.IsNullOrEmpty(tokenManagerConfiguration.AadEndpoint)
                || string.IsNullOrEmpty(tokenManagerConfiguration.TargetResource)
                || string.IsNullOrEmpty(tokenManagerConfiguration.TenantId))
                {
                    var ex = new InvalidOperationException($"[RPaaS Init] Please make sure '{nameof(MetaRPOptions)}' is set in the configuration.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var certStore = sp.GetService<CertificateStore>();
                if (certStore == null)
                {
                    var ex = new InvalidOperationException("[RPaaS Init] Cannot find a certificate store in the dependency injection container to initizlize RPaaS client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var tokenManager = new TokenManager.TokenManager(tokenManagerConfiguration, certStore);

                var httpClientFactory = sp.GetService<IHttpClientFactory>();
                if (httpClientFactory == null)
                {
                    var ex = new InvalidOperationException("[RPaaS Init] Cannot find a httpClientFactory instance to initizlize RPaaS client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var metaRPClient = new MetaRPStorageClient(metaRPOptions.MetaRPEndpoint, httpClientFactory.CreateClient(), () =>
                {
                    return tokenManager.GetTokenAsync(metaRPOptions.KeyVaultEndpoint, metaRPOptions.AccessorClientId, metaRPOptions.AccessorCertificateName);
                });

                return metaRPClient;
            });
        }
    }
}
