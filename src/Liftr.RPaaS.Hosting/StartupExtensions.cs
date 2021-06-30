//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        public static void AddRPaaSAuthentication(this IServiceCollection services, RPaaSAuthOptions authOptions)
        {
            if (authOptions == null)
            {
                throw new ArgumentNullException(nameof(authOptions));
            }

            services.AddAuthentication(options => options.DefaultScheme = AzureADDefaults.JwtBearerAuthenticationScheme)
                .AddAzureADBearer(options =>
                {
                    options.Instance = authOptions.Instance.OriginalString;
                    options.TenantId = authOptions.TenantId;
                    options.ClientId = authOptions.ClientId;
                });

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
