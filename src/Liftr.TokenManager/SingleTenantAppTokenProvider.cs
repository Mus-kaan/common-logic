//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    public sealed class SingleTenantAppTokenProvider : ISingleTenantAppTokenProvider
    {
        private readonly SingleTenantAADAppTokenProviderOptions _options;
        private readonly CertificateStore _certStore;
        private readonly TokenManager _tokenManager;

        public SingleTenantAppTokenProvider(SingleTenantAADAppTokenProviderOptions tokenProviderOptions, IKeyVaultClient kvClient, Serilog.ILogger logger)
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
                TenantId = _options.TenantId,
            };

            _tokenManager = new TokenManager(tmOptions, _certStore);

            logger.LogInformation($"Run '{nameof(GetTokenAsync)}' to make sure the provider is initialized correctly.");
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
            var token = GetTokenAsync().Result;
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Cannot load token.");
            }
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
        }

        public void Dispose()
        {
            _certStore.Dispose();
        }

        public async Task<string> GetTokenAsync()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var cert = await _certStore.GetCertificateAsync(_options.KeyVaultEndpoint, _options.CertificateName);
#pragma warning restore CA2000 // Dispose objects before losing scope
            return await _tokenManager.GetTokenAsync(_options.ApplicationId, cert, sendX5c: true);
        }
    }
}
