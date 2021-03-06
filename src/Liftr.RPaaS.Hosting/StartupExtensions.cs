//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.ServiceEssentials.Extensions.AspNetCoreMiddleware;
using Microsoft.IdentityModel.S2S.Extensions.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Liftr.TokenManager;
using System;
using System.IdentityModel.Tokens.Jwt;
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

                var tokenProvider = GetTokenProvider(sp);

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

        public static void AddMetaRPClientWithTokenProvider(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddSingleton<IMultiTenantAppTokenProvider, MultiTenantAppTokenProvider>((sp) =>
            {
                return GetTokenProvider(sp);
            });

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

                var tokenProvider = sp.GetService<IMultiTenantAppTokenProvider>();
                if (tokenProvider == null)
                {
                    var ex = new InvalidOperationException("Cannot find IMultiTenantAppTokenProvider in the dependency injection container to initizlize RPaaS client.");
                    logger.LogError(ex.Message);
                    throw ex;
                }

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

        public static void AddRPaaSAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var authOptions = new RPaaSAuthOptions();
            configuration.GetSection(nameof(RPaaSAuthOptions)).Bind(authOptions);

            if (authOptions.ShouldSkip)
            {
                // always pass authentication by adding an "always success" scheme
                services.AddAuthentication(options => options.DefaultAuthenticateScheme = "AlwaysSuccess")
                    .AddScheme<AuthenticationSchemeOptions, AlwaysSuccessAuthenticationHandler>("AlwaysSuccess", null);

                // always pass authorization by adding an "always success" policy as "RPaaS"
                services.AddSingleton<IAuthorizationHandler, AlwaysSuccessAuthorizationHandler>();
                services.AddAuthorization(options =>
                {
                    options.AddPolicy(RPaaSAuthConstants.RPaaSAuthorizationRule, policy => policy.Requirements.Add(new AlwaysSuccessRequirement()));
                });
                return;
            }

            services.AddAuthentication(options => options.DefaultScheme = S2SAuthenticationDefaults.AuthenticationScheme)
                    .AddMiseWithDefaultAuthentication(configuration);

            services.Configure<JwtBearerOptions>(AzureADDefaults.JwtBearerAuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidAudience = authOptions.Audience;
                options.TokenValidationParameters.IssuerValidator = ValidateAadIssuer;
                options.TokenValidationParameters.RequireSignedTokens = true;
                options.TokenValidationParameters.RequireExpirationTime = true;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ValidateTokenReplay = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(RPaaSAuthConstants.RPaaSAuthorizationRule, policy => policy.RequireClaim("appid", new[] { authOptions.RPaaSAppId }));
            });
        }

        private static MultiTenantAppTokenProvider GetTokenProvider(IServiceProvider sp)
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

            return tokenProvider;
        }

        private static string ValidateAadIssuer(string issuer, SecurityToken token, TokenValidationParameters options)
        {
            var jwtToken = token as JwtSecurityToken;
            if (jwtToken == null)
            {
                throw new SecurityTokenInvalidIssuerException("Issuer validation failed. Expected a JWT Token from Azure Active Directory.");
            }

            // When AzureAdOptions.TenantId is set to "common", the valid issuers are templated with {tenantid},
            // e.g. "https://sts.windows-ppe.net/{tenantid}/". We need to replace the "{tenantid}" substring with
            // the actual value.
            var tenantId = jwtToken.Payload["tid"].ToString();
            foreach (var iss in options.ValidIssuers)
            {
                var candidateIssuer = iss.Replace("{tenantid}", tenantId, StringComparison.OrdinalIgnoreCase);
                if (candidateIssuer.OrdinalEquals(issuer))
                {
                    // Valid issuer
                    return candidateIssuer;
                }
            }

            throw new SecurityTokenInvalidIssuerException($"Issuer validation failed. Issuer {issuer} is not a valid issuer.");
        }
    }
}
