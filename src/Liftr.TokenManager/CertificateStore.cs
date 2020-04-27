//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    /// <summary>
    /// This class is used to read the certificate from the keyvault and store it
    /// </summary>
    public sealed class CertificateStore : IDisposable
    {
        private readonly IKeyVaultClient _kvClient;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _mu = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, X509Certificate2> _certificates = new Dictionary<string, X509Certificate2>();

        public CertificateStore(IKeyVaultClient kvClient, ILogger logger)
        {
            _kvClient = kvClient ?? throw new ArgumentNullException(nameof(kvClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Dispose()
        {
            _mu.Dispose();
        }

        public async Task<X509Certificate2> GetCertificateAsync(Uri keyVaultEndpoint, string certificateName)
        {
            if (keyVaultEndpoint == null)
            {
                throw new ArgumentNullException(nameof(keyVaultEndpoint));
            }

            var certPath = $"{keyVaultEndpoint.AbsoluteUri}/certificates/{certificateName}";

            await _mu.WaitAsync();
            try
            {
                if (!_certificates.ContainsKey(certPath))
                {
                    _certificates[certPath] = await LoadCertificateFromKeyVaultAsync(keyVaultEndpoint, certificateName);
                }

                return _certificates[certPath];
            }
            finally
            {
                _mu.Release();
            }
        }

        private async Task<X509Certificate2> LoadCertificateFromKeyVaultAsync(Uri keyVaultEndpoint, string certificateName)
        {
            using (var operation = _logger.StartTimedOperation(nameof(LoadCertificateFromKeyVaultAsync)))
            {
                operation.SetContextProperty(nameof(certificateName), certificateName);
                try
                {
                    _logger.Information("Start loading certificate with name {CertificateName} ...", certificateName);
                    var secretBundle = await _kvClient.GetSecretAsync(keyVaultEndpoint.AbsoluteUri, certificateName);
                    _logger.Information("Loaded the certificate with name {CertificateName} from key vault with endpoint {KeyVaultEndpoint}", certificateName, keyVaultEndpoint.AbsoluteUri);
                    var privateKeyBytes = Convert.FromBase64String(secretBundle.Value);
                    return new X509Certificate2(privateKeyBytes);
                }
                catch (Exception ex)
                {
                    operation.FailOperation();
                    _logger.Fatal(ex, "Failed to load certificate: {certificateName} from keyvault {keyvault}", certificateName, keyVaultEndpoint.AbsoluteUri);
                    throw;
                }
            }
        }
    }
}
