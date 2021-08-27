//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Marketplace.Agreement.Interfaces;
using Microsoft.Liftr.Marketplace.Agreement.Options;
using Microsoft.Liftr.Marketplace.Agreement.Service;
using Microsoft.Liftr.Marketplace.ARM;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.ARM.Options;
using Microsoft.Liftr.Marketplace.Billing;
using Microsoft.Liftr.Marketplace.Billing.Contracts;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;
using Microsoft.Liftr.Marketplace.Saas.Options;
using Microsoft.Liftr.Marketplace.TokenService;
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
                    var ex = new InvalidOperationException("Cannot find a key vault client in the dependency injection container to initialize Marketplace Fulfillment client.");
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
                    var ex = new InvalidOperationException("[Marketplace ARM Init] Cannot find a key vault client in the dependency injection container");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var httpClientFactory = sp.GetService<IHttpClientFactory>();
                if (httpClientFactory == null)
                {
                    var ex = new InvalidOperationException("[Marketplace ARM Init] Cannot find a httpClientFactory instance to initialize Marketplace ARM client.");
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
        /// This method adds the Marketplace ARM Client behavior using Token service where caller passes config to to get FPA Token or cert object for create/delete
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="consoleLog">Generate a console logger without retrieving the logger from DI container.</param>
        public static void AddMarketplaceARMClientWithTokenService(this IServiceCollection services, IConfiguration configuration, bool consoleLog = false)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MarketplaceARMClientAuthOptions>(configuration.GetSection(nameof(MarketplaceARMClientAuthOptions)));
            services.Configure<MarketplaceARMClientAuthOptions>((saasOptions) =>
            {
                saasOptions.AuthOptions.KeyVaultEndpoint = new Uri(configuration[GlobalSettingConstants.VaultEndpoint]);
            });

            services.AddSingleton<ITokenServiceRestClient, TokenServiceRestClient>(sp =>
            {
                var options = sp.GetService<IOptions<MarketplaceARMClientAuthOptions>>().Value;
                var logger = consoleLog ? Liftr.Logging.LoggerFactory.ConsoleLogger : sp.GetService<Serilog.ILogger>();

                var tokenServiceOptions = options.AuthOptions;
                if (!Validate(tokenServiceOptions))
                {
                    var ex = new InvalidOperationException($"Please make sure '{nameof(MarketplaceARMClientAuthOptions.AuthOptions)}' is set under the '{nameof(MarketplaceARMClientAuthOptions)}' section.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                logger.Information($"[Marketplace Agreement Init] Service Dependency Injection request for Marketplace Auth is made by Cert: {tokenServiceOptions.CertificateName} and Application: {tokenServiceOptions.ApplicationId}");

                var kvClient = sp.GetService<IKeyVaultClient>();
                if (kvClient == null)
                {
                    var ex = new InvalidOperationException("[Marketplace ARM Init] Cannot find a key vault client in the dependency injection container");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var httpClientFactory = sp.GetService<IHttpClientFactory>();
                if (httpClientFactory == null)
                {
                    var ex = new InvalidOperationException("[Marketplace ARM Init] Cannot find a httpClientFactory instance to initialize Marketplace ARM client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var tokenProvider = new SingleTenantAppTokenProvider(tokenServiceOptions, kvClient, logger);

                return new TokenServiceRestClient(options.TokenServiceAPI.Endpoint, options.TokenServiceAPI.ApiVersion, logger, httpClientFactory, () => tokenProvider.GetTokenAsync());
            });

            services.AddSingleton<IMarketplaceARMClient, MarketplaceARMClient>(sp =>
            {
                var logger = consoleLog ? Liftr.Logging.LoggerFactory.ConsoleLogger : sp.GetService<Serilog.ILogger>();
                var options = sp.GetService<IOptions<MarketplaceARMClientAuthOptions>>().Value;

                var tokenServiceRestClient = sp.GetService<ITokenServiceRestClient>();
                if (tokenServiceRestClient == null)
                {
                    var ex = new InvalidOperationException("[Marketplace ARM Init] Cannot find a tokenServiceRestClient in the dependency injection container");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var httpClientFactory = sp.GetService<IHttpClientFactory>();
                if (httpClientFactory == null)
                {
                    var ex = new InvalidOperationException("[Marketplace ARM Init] Cannot find a httpClientFactory instance to initialize Marketplace ARM client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var marketplaceRestClient = new MarketplaceRestClient(options.API.Endpoint, options.API.ApiVersion, logger, httpClientFactory, () => tokenServiceRestClient.GetTokenAsync());
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
                    var ex = new InvalidOperationException("[Marketplace Billing Init] Cannot find a httpClientFactory instance to initialize Marketplace Billing client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var tokenProvider = new SingleTenantAppTokenProvider(saasTechnicalConfig, kvClient, logger);
                return new MarketplaceBillingClient(marketplaceOptions.API, () => tokenProvider.GetTokenAsync(), logger, httpClientFactory);
            });
        }

        /// <summary>
        /// This method adds the Marketplace Agreement Client for signing the agreement before SaaS resource creation
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="consoleLog">Generate a console logger without retrieving the logger from DI container.</param>
        public static void AddMarketplaceAgreementClient(this IServiceCollection services, IConfiguration configuration, bool consoleLog = false)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MarketplaceAgreementOptions>(configuration.GetSection(nameof(MarketplaceAgreementOptions)));
            services.Configure<MarketplaceAgreementOptions>((mpARMOptions) =>
            {
                mpARMOptions.AuthOptions.KeyVaultEndpoint = new Uri(configuration[GlobalSettingConstants.VaultEndpoint]);
            });

            services.AddSingleton<ISignAgreementRestClient, SignAgreementRestClient>(sp =>
            {
                var agreementOptions = sp.GetService<IOptions<MarketplaceAgreementOptions>>().Value;
                var logger = consoleLog ? Liftr.Logging.LoggerFactory.ConsoleLogger : sp.GetService<Serilog.ILogger>();

                if (!Validate(agreementOptions))
                {
                    var ex = new InvalidOperationException($"Please make sure '{nameof(MarketplaceAgreementOptions)}' is set properly.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var kvClient = sp.GetService<IKeyVaultClient>();
                if (kvClient == null)
                {
                    var ex = new InvalidOperationException("[Marketplace Agreement Init] Cannot find a key vault client in the dependency injection container");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var certStore = new CertificateStore(kvClient, logger);

                return new SignAgreementRestClient(agreementOptions, logger, certStore);
            });

            services.AddSingleton<ISignAgreementService, SignAgreementService>(sp =>
            {
                var logger = consoleLog ? Liftr.Logging.LoggerFactory.ConsoleLogger : sp.GetService<Serilog.ILogger>();

                var signAgreementRestClient = sp.GetService<ISignAgreementRestClient>();
                if (signAgreementRestClient == null)
                {
                    var ex = new InvalidOperationException("[Marketplace Agreement Init] Cannot find a signAgreementRestClient in the dependency injection container");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                return new SignAgreementService(
                    signAgreementRestClient,
                    logger);
            });
        }

        /// <summary>
        /// This method adds the Marketplace Agreement Client for signing the agreement before SaaS resource creation
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="consoleLog">Generate a console logger without retrieving the logger from DI container.</param>
        public static void AddMarketplaceAgreementClientWithTokenService(this IServiceCollection services, IConfiguration configuration, bool consoleLog = false)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MarketplaceAgreementClientAuthOptions>(configuration.GetSection(nameof(MarketplaceAgreementClientAuthOptions)));
            services.Configure<MarketplaceAgreementClientAuthOptions>((saasOptions) =>
            {
                saasOptions.AuthOptions.KeyVaultEndpoint = new Uri(configuration[GlobalSettingConstants.VaultEndpoint]);
            });

            services.AddSingleton<ITokenServiceRestClient, TokenServiceRestClient>(sp =>
            {
                var options = sp.GetService<IOptions<MarketplaceAgreementClientAuthOptions>>().Value;
                var logger = consoleLog ? Logging.LoggerFactory.ConsoleLogger : sp.GetService<Serilog.ILogger>();

                var tokenServiceOptions = options.AuthOptions;
                if (!Validate(tokenServiceOptions))
                {
                    var ex = new InvalidOperationException($"Please make sure '{nameof(MarketplaceAgreementClientAuthOptions.AuthOptions)}' is set under the '{nameof(MarketplaceAgreementClientAuthOptions)}' section.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                logger.Information($"[Marketplace Agreement Init] Service Dependency Injection request for Marketplace Auth is made by Cert: {tokenServiceOptions.CertificateName} and Application: {tokenServiceOptions.ApplicationId}");

                var kvClient = sp.GetService<IKeyVaultClient>();
                if (kvClient == null)
                {
                    var ex = new InvalidOperationException("[Marketplace Agreement Init] Cannot find a key vault client in the dependency injection container");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var httpClientFactory = sp.GetService<IHttpClientFactory>();
                if (httpClientFactory == null)
                {
                    var ex = new InvalidOperationException("[Marketplace Agreement Init] Cannot find a httpClientFactory instance to initialize Marketplace Agreement client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var tokenProvider = new SingleTenantAppTokenProvider(tokenServiceOptions, kvClient, logger);

                return new TokenServiceRestClient(options.TokenServiceAPI.Endpoint, options.TokenServiceAPI.ApiVersion, logger, httpClientFactory, () => tokenProvider.GetTokenAsync());
            });

            services.AddSingleton<ISignAgreementRestClient, SignAgreementRestClient>(sp =>
            {
                var options = sp.GetService<IOptions<MarketplaceAgreementClientAuthOptions>>().Value;
                var logger = consoleLog ? Liftr.Logging.LoggerFactory.ConsoleLogger : sp.GetService<Serilog.ILogger>();

                if (!Validate(options.AuthOptions))
                {
                    var ex = new InvalidOperationException($"Please make sure '{nameof(MarketplaceAgreementClientAuthOptions)}' is set properly.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                var httpClientFactory = sp.GetService<IHttpClientFactory>();
                if (httpClientFactory == null)
                {
                    var ex = new InvalidOperationException("[Marketplace Agreement Init] Cannot find a httpClientFactory instance to initialize Marketplace Agreement client.");
                    logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var tokenServiceRestClient = sp.GetService<ITokenServiceRestClient>();
                if (tokenServiceRestClient == null)
                {
                    var ex = new InvalidOperationException("[Marketplace Agreement Init] Cannot find a tokenServiceRestClient in the dependency injection container");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                return new SignAgreementRestClient(options.API.Endpoint, options.API.ApiVersion, logger, httpClientFactory, () => tokenServiceRestClient.GetCertificateAsync());
            });

            services.AddSingleton<ISignAgreementService, SignAgreementService>(sp =>
            {
                var logger = consoleLog ? Logging.LoggerFactory.ConsoleLogger : sp.GetService<Serilog.ILogger>();

                var signAgreementRestClient = sp.GetService<ISignAgreementRestClient>();
                if (signAgreementRestClient == null)
                {
                    var ex = new InvalidOperationException("[Marketplace Agreement Init] Cannot find a signAgreementRestClient in the dependency injection container");
                    logger.LogError(ex.Message);
                    throw ex;
                }

                return new SignAgreementService(
                    signAgreementRestClient,
                    logger);
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

        private static bool Validate(MarketplaceAgreementOptions options)
        {
            if (options == null
                || options.API.Endpoint == null
                || string.IsNullOrWhiteSpace(options.API.ApiVersion)
                || options.AuthOptions == null
                || options.AuthOptions.KeyVaultEndpoint == null
                || string.IsNullOrWhiteSpace(options.AuthOptions.CertificateName))
            {
                return false;
            }

            return true;
        }
    }
}
