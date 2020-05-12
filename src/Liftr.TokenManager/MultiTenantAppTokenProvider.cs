//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Liftr.TokenManager.Options;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    public sealed class MultiTenantAppTokenProvider : IMultiTenantAppTokenProvider
    {
        private readonly AADAppTokenProviderOptions _options;
        private readonly CertificateStore _certStore;
        private readonly TokenManager _tokenManager;

        public MultiTenantAppTokenProvider(AADAppTokenProviderOptions tokenProviderOptions, IKeyVaultClient kvClient, Serilog.ILogger logger)
        {
            _options = tokenProviderOptions ?? throw new ArgumentNullException(nameof(tokenProviderOptions));
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _certStore = new CertificateStore(kvClient, logger);

            var tmOptions = new TokenManagerConfiguration()
            {
                TargetResource = _options.TargetResource,
                AadEndpoint = _options.AadEndpoint,
            };

            _tokenManager = new TokenManager(tmOptions, _certStore);

            logger.LogInformation($"Load certificate to make sure the provider is initialized correctly.");
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
            var cert = _certStore.GetCertificateAsync(_options.KeyVaultEndpoint, _options.CertificateName).Result;
            if (cert == null)
            {
                throw new InvalidOperationException("Cannot load certificate.");
            }
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
        }

        public void Dispose()
        {
            _certStore.Dispose();
        }

        public async Task<string> GetTokenAsync(string tenantId)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var cert = await _certStore.GetCertificateAsync(_options.KeyVaultEndpoint, _options.CertificateName);
#pragma warning restore CA2000 // Dispose objects before losing scope
            return await _tokenManager.GetTokenAsync(_options.ApplicationId, cert, tenantId, sendX5c: true);
        }
    }
}
