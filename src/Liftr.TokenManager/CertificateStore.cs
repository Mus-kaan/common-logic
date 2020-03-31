//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Liftr.TokenManager
{
    /// <summary>
    /// This class is used to read the certificate from the keyvault and store it
    /// </summary>
    public class CertificateStore
    {
        private readonly IKeyVaultClient _kvClient;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, X509Certificate2> _certificates;

        public CertificateStore(IKeyVaultClient kvClient, ILogger logger)
        {
            _kvClient = kvClient ?? throw new ArgumentNullException(nameof(kvClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _certificates = new ConcurrentDictionary<string, X509Certificate2>();
        }

        public async Task<X509Certificate2> GetCertificateAsync(Uri keyVaultEndpoint, string certificateName)
        {
            if (keyVaultEndpoint == null)
            {
                throw new ArgumentNullException(nameof(keyVaultEndpoint));
            }

            var certPath = $"{keyVaultEndpoint.AbsoluteUri}/certificates/{certificateName}";

            if (!_certificates.TryGetValue(certPath, out var certificate))
            {
                try
                {
                    _logger.Information("Start loading certificate with name {CertificateName} ...", certificateName);
                    var secretBundle = await _kvClient.GetSecretAsync(keyVaultEndpoint.AbsoluteUri, certificateName);
                    _logger.Information("Loaded the certificate with name {CertificateName} from key vault with endpoint {KeyVaultEndpoint}", certificateName, keyVaultEndpoint.AbsoluteUri);
                    var privateKeyBytes = Convert.FromBase64String(secretBundle.Value);
                    certificate = new X509Certificate2(privateKeyBytes);
                    _certificates.TryAdd(certPath, certificate);
                }
                catch (Exception ex)
                {
                    _logger.Fatal(ex, "Failed to load certificate: {certificateName} from keyvault {keyvault}", certificateName, keyVaultEndpoint.AbsoluteUri);
                    throw;
                }
            }

            return certificate;
        }
    }
}
