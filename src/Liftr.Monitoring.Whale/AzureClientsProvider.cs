//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Monitoring.Whale;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Options;
using Microsoft.Liftr.TokenManager;
using Microsoft.Rest;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Service
{
    public class AzureClientsProvider : IAzureClientsProvider
    {
        private readonly CertificateStore _certificateStore;
        private readonly AzureClientsProviderOptions _providerOptions;
        private readonly ITokenManager _tokenManager;
        private readonly ILogger _logger;

        public AzureClientsProvider(
            CertificateStore certificateStore,
            IOptions<AzureClientsProviderOptions> options,
            ILogger logger)
        {
            _certificateStore = certificateStore ?? throw new ArgumentNullException(nameof(certificateStore));
            _providerOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _tokenManager = new TokenManager.TokenManager(_providerOptions.TokenManagerConfiguration);
        }

        public async Task<IResourceGraphClient> GetResourceGraphClientAsync(string tenantId)
        {
            _logger.Information(
                "Obtaining token for tenant {@tenantId}.", tenantId);

            var token = await _tokenManager.GetTokenAsync(
                _providerOptions.KeyVaultEndpoint, _providerOptions.ClientId, _providerOptions.CertificateName, tenantId, sendX5c: true);

            _logger.Information(
                "Obtained token for tenant {@tenantId}.", tenantId);

            return new ResourceGraphClient(_providerOptions.ArmEndpoint, new TokenCredentials(token));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        public async Task<IAzure> GetFluentClientAsync(string subscriptionId, string tenantId)
        {
            _logger.Information(
                "Obtaining fluent client for subscription {@subscriptionId} at tenant {@tenantId}.", subscriptionId, tenantId);

            var certificate = await _certificateStore.GetCertificateAsync(
                _providerOptions.KeyVaultEndpoint, _providerOptions.CertificateName);

            var delegatingHandler = new WhaleDelegatingHandler(
                _tokenManager, _providerOptions);

            // Using this flag will allow authenticating with the FPA as the rollover is enabled for that cert
            var isCertificateRollOverEnabled = true;

            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                _providerOptions.ClientId,
                certificate,
                isCertificateRollOverEnabled,
                tenantId,
                AzureEnvironment.AzureGlobalCloud);

            var fluentClient = Azure.Management.Fluent.Azure
                .Configure()
                .WithDelegatingHandler(delegatingHandler)
                .Authenticate(credentials)
                .WithSubscription(subscriptionId);

            _logger.Information(
                "Obtained fluent client for subscription {@subscriptionId} at tenant {@tenantId}.", subscriptionId, tenantId);

            return fluentClient;
        }
    }
}
