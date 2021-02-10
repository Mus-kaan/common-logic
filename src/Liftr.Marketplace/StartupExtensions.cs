//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Marketplace.ARM;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.ARM.Options;
using Microsoft.Liftr.Marketplace.Billing;
using Microsoft.Liftr.Marketplace.Billing.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;
using Microsoft.Liftr.Marketplace.Saas.Options;
using Microsoft.Liftr.TokenManager;
using Microsoft.Liftr.TokenManager.Options;
using System;
using System.Net.Http;

namespace Microsoft.Liftr.Marketplace.Saas
{
    public static class StartupExtensions
    {
        /// <summary>
        /// This method adds the Marketplace Fulfillment Client which can be used by the RP to handle the fulfillment on behalf of the partner
        /// </summary>
        /// <remarks>
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2
        /// </remarks>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="consoleLog">Generate a console logger without retrieving the logger from DI container.</param>
        public static void AddMarketplaceFulfillmentClient(this IServiceCollection services, IConfiguration configuration, bool consoleLog = false)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MarketplaceSaasOptions>(configuration.GetSection(nameof(MarketplaceSaasOptions)));
            services.Configure<MarketplaceSaasOptions>((saasOptions) =>
            {
                saasOptions.SaasOfferTechnicalConfig.KeyVaultEndpoint = new Uri(configuration[GlobalSettingConstants.VaultEndpoint]);
            });

            services.AddSingleton<IMarketplaceFulfillmentClient, MarketplaceFulfillmentClient>(sp =>
            {
                var logger = consoleLog ? Liftr.Logging.LoggerFactory.ConsoleLogger : sp.GetService<Serilog.ILogger>();
                var marketplaceOptions = sp.GetService<IOptions<MarketplaceSaasOptions>>().Value;

                if (marketplaceOptions == null)
                {
                    var ex = new InvalidOperationException($"[Marketplace Fulfillment Init] Please make sure '{nameof(MarketplaceSaasOptions)}' is set in the configuration.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var saasTechnicalConfig = marketplaceOptions.SaasOfferTechnicalConfig;
                if (!Validate(saasTechnicalConfig))
                {
                    var ex = new InvalidOperationException($"Please make sure '{nameof(MarketplaceSaasOptions.SaasOfferTechnicalConfig)}' is set under the '{nameof(MarketplaceSaasOptions)}' section.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var kvClient = sp.GetService<IKeyVaultClient>();
                if (kvClient == null)
                {
                    var ex = new InvalidOperationException("Cannot find a key vault client in the dependency injection container to initizlize Marketplace Fulfillment client.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var httpClientFactory = sp.GetService<IHttpClientFactory>();
                if (httpClientFactory == null)
                {
                    var ex = new InvalidOperationException("[Marketplace Fulfillment Init] Cannot find a httpClientFactory instance to initialize Marketplace Fulfillment client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var tokenProvider = new SingleTenantAppTokenProvider(saasTechnicalConfig, kvClient, logger);
                var marketplaceRestClient = new MarketplaceRestClient(marketplaceOptions.API.Endpoint, marketplaceOptions.API.ApiVersion, logger, httpClientFactory, async () => await tokenProvider.GetTokenAsync());
                return new MarketplaceFulfillmentClient(marketplaceRestClient, logger);
            });
        }

        /// <summary>
        /// This method adds the Marketplace ARM Client inorder to create the Microsoft.Saas resource by calling ARM
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="consoleLog">Generate a console logger without retrieving the logger from DI container.</param>
        public static void AddMarketplaceARMClient(this IServiceCollection services, IConfiguration configuration, bool consoleLog = false)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MarketplaceARMClientOptions>(configuration.GetSection(nameof(MarketplaceARMClientOptions)));
            services.Configure<MarketplaceARMClientOptions>((mpARMOptions) =>
            {
                mpARMOptions.MarketplaceFPAOptions.KeyVaultEndpoint = new Uri(configuration[GlobalSettingConstants.VaultEndpoint]);
            });

            services.AddSingleton<IMarketplaceARMClient, MarketplaceARMClient>(sp =>
            {
                var options = sp.GetService<IOptions<MarketplaceARMClientOptions>>().Value;
                var logger = consoleLog ? Liftr.Logging.LoggerFactory.ConsoleLogger : sp.GetService<Serilog.ILogger>();

                var fpaOptions = options.MarketplaceFPAOptions;
                if (!Validate(fpaOptions))
                {
                    var ex = new InvalidOperationException($"Please make sure '{nameof(MarketplaceARMClientOptions.MarketplaceFPAOptions)}' is set under the '{nameof(MarketplaceARMClientOptions)}' section.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var kvClient = sp.GetService<IKeyVaultClient>();
                if (kvClient == null)
                {
                    var ex = new InvalidOperationException("Marketplace ARM Init] Cannot find a key vault client in the dependency injection container");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var httpClientFactory = sp.GetService<IHttpClientFactory>();
                if (httpClientFactory == null)
                {
                    var ex = new InvalidOperationException("[Marketplace ARM Init] Cannot find a httpClientFactory instance to initizlize Marketplace ARM client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var tokenProvider = new SingleTenantAppTokenProvider(fpaOptions, kvClient, logger);
                var marketplaceRestClient = new MarketplaceRestClient(options.API.Endpoint, options.API.ApiVersion, logger, httpClientFactory, () => tokenProvider.GetTokenAsync());
                return new MarketplaceARMClient(
                    logger,
                    marketplaceRestClient);
            });
        }

        /// <summary>
        /// This method adds the Marketplace Metered Billing Client which is used to submit the billing usage to marketplace on behalf of the partner
        /// </summary>
        /// <remarks>
        /// https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis
        /// </remarks>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="consoleLog">Generate a console logger without retrieving the logger from DI container.</param>
        public static void AddMarketplaceBillingClient(this IServiceCollection services, IConfiguration configuration, bool consoleLog = false)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MarketplaceSaasOptions>(configuration.GetSection(nameof(MarketplaceSaasOptions)));
            services.Configure<MarketplaceSaasOptions>((saasOptions) =>
            {
                saasOptions.SaasOfferTechnicalConfig.KeyVaultEndpoint = new Uri(configuration[GlobalSettingConstants.VaultEndpoint]);
            });

            services.AddSingleton<IMarketplaceBillingClient, MarketplaceBillingClient>(sp =>
            {
                var logger = consoleLog ? Liftr.Logging.LoggerFactory.ConsoleLogger : sp.GetService<Serilog.ILogger>();
                var marketplaceOptions = sp.GetService<IOptions<MarketplaceSaasOptions>>().Value;

                if (marketplaceOptions == null)
                {
                    var ex = new InvalidOperationException($"[Marketplace Billing Init] Please make sure '{nameof(MarketplaceSaasOptions)}' is set in the configuration.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var saasTechnicalConfig = marketplaceOptions.SaasOfferTechnicalConfig;
                if (!Validate(saasTechnicalConfig))
                {
                    var ex = new InvalidOperationException($"Please make sure '{nameof(MarketplaceSaasOptions.SaasOfferTechnicalConfig)}' is set under the '{nameof(MarketplaceSaasOptions)}' section.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var kvClient = sp.GetService<IKeyVaultClient>();
                if (kvClient == null)
                {
                    var ex = new InvalidOperationException("[Marketplace Billing Init]: Cannot find a key vault client in the dependency injection container");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var httpClientFactory = sp.GetService<IHttpClientFactory>();
                if (httpClientFactory == null)
                {
                    var ex = new InvalidOperationException("[Marketplace Billing Init] Cannot find a httpClientFactory instance to initizlize Marketplace Billing client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var tokenProvider = new SingleTenantAppTokenProvider(saasTechnicalConfig, kvClient, logger);
                return new MarketplaceBillingClient(marketplaceOptions.API, () => tokenProvider.GetTokenAsync(), logger, httpClientFactory);
            });
        }

        private static bool Validate(SingleTenantAADAppTokenProviderOptions options)
        {
            if (options == null
                || options.KeyVaultEndpoint == null
                || string.IsNullOrWhiteSpace(options.AadEndpoint)
                || string.IsNullOrWhiteSpace(options.TargetResource)
                || string.IsNullOrWhiteSpace(options.ApplicationId)
                || string.IsNullOrWhiteSpace(options.CertificateName)
                || string.IsNullOrWhiteSpace(options.TenantId))
            {
                return false;
            }

            return true;
        }
    }
}
