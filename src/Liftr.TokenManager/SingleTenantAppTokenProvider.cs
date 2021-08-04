//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Liftr.TokenManager.Options;
using System;
using System.Threading;
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

            _options.CheckValues();

            _certStore = new CertificateStore(kvClient, logger);

            var tmOptions = new TokenManagerConfiguration()
            {
                TargetResource = _options.TargetResource,
                AadEndpoint = _options.AadEndpoint,
                TenantId = _options.TenantId,
            };

            _tokenManager = new TokenManager(tmOptions, _certStore);

            logger.Information($"Run '{nameof(GetTokenAsync)}' to make sure the provider is initialized correctly.");
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
            _tokenManager.Dispose();
            _certStore.Dispose();
        }

        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            return await _tokenManager.GetTokenAsync(
                _options.KeyVaultEndpoint,
                _options.ApplicationId,
                _options.CertificateName,
                sendX5c: true,
                cancellationToken: cancellationToken);
        }
    }
}
