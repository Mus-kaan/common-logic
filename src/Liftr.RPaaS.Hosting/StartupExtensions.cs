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

                if (metaRPOptions == null)
                {
                    var ex = new InvalidOperationException($"Please make sure '{nameof(MetaRPOptions)}' section is set in the appsettings.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var kvClient = sp.GetService<IKeyVaultClient>();
                if (kvClient == null)
                {
                    var ex = new InvalidOperationException("Cannot find a key vault client in the dependency injection container to initizlize RPaaS client.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var fpaOptions = metaRPOptions.FPAOptions;
                if (fpaOptions == null
                || fpaOptions.KeyVaultEndpoint == null
                || string.IsNullOrEmpty(fpaOptions.AadEndpoint)
                || string.IsNullOrEmpty(fpaOptions.TargetResource)
                || string.IsNullOrEmpty(fpaOptions.ApplicationId)
                || string.IsNullOrEmpty(fpaOptions.CertificateName))
                {
                    var ex = new InvalidOperationException($"Please make sure '{nameof(MetaRPOptions.FPAOptions)}' is set under the '{nameof(MetaRPOptions)}' section.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                if (string.IsNullOrEmpty(metaRPOptions.UserRPTenantId))
                {
                    var ex = new InvalidOperationException($"Please make sure '{nameof(MetaRPOptions.UserRPTenantId)}' is set under the '{nameof(MetaRPOptions)}' section.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var tokenProvider = new MultiTenantAppTokenProvider(fpaOptions, kvClient, logger);

                var httpClientFactory = sp.GetService<IHttpClientFactory>();
                if (httpClientFactory == null)
                {
                    var ex = new InvalidOperationException("Cannot find a httpClientFactory instance to initizlize RPaaS client.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var metaRPClient = new MetaRPStorageClient(
                    httpClientFactory.CreateClient(),
                    metaRPOptions,
                    (tenantId) =>
                    {
                        return tokenProvider.GetTokenAsync(tenantId);
                    },
                    logger);

                return metaRPClient;
            });
        }
    }
}
