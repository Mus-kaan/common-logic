//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.AzureAd.Icm.Types;
using Microsoft.AzureAd.Icm.WebService.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.IcmConnector;
using Microsoft.Liftr.TokenManager;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Prom2IcM
{
    public sealed class ICMClientProvider : BackgroundService, IICMClientProvider
    {
        private readonly ICMClientOptions _options;
        private readonly Serilog.ILogger _logger;
        private readonly CertificateStore _certStore;
        private ITaskBasedConnector _icmClient;

        public ICMClientProvider(IOptions<ICMClientOptions> options, IKeyVaultClient kvClient, Serilog.ILogger logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _certStore = new CertificateStore(kvClient, logger);

            _options.CheckValid();
        }

        public sealed override void Dispose()
        {
            _certStore.Dispose();
            _icmClient.Dispose();
            base.Dispose();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "<Pending>")]
        public ICMClientOptions GetClientOptions()
        {
            return _options;
        }

        public async Task<ITaskBasedConnector> GetICMClientAsync()
        {
            if (_icmClient == null)
            {
                await LoadCertificateAndGenerateIcmClientAsync();
            }

            return _icmClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information($"{nameof(ICMClientProvider)}_started");
            while (!stoppingToken.IsCancellationRequested)
            {
                await LoadCertificateAndGenerateIcmClientAsync();
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        private async Task LoadCertificateAndGenerateIcmClientAsync()
        {
            _logger.Information("Start loading certificate and regenerate the IcM connector client.");

            var cert = await _certStore.GetCertificateAsync(new Uri(_options.KeyVaultEndpoint), _options.IcmConnectorCertificateName);

            _icmClient = ConnectorClientFactory.CreateAsyncClient(_options.ICMConnectorEndpoint, cert);
        }
    }
}
