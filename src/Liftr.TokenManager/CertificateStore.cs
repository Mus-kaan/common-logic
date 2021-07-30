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
        private readonly TimeSpan _certificateCacheTTL;
        private readonly SemaphoreSlim _mu = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, CertificateCacheItem> _cachedCertificates = new Dictionary<string, CertificateCacheItem>();

        public CertificateStore(IKeyVaultClient kvClient, ILogger logger, TimeSpan? certificateCacheTTL = null)
        {
            _kvClient = kvClient ?? throw new ArgumentNullException(nameof(kvClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _certificateCacheTTL = certificateCacheTTL ?? TimeSpan.FromMinutes(30);
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
                if (_cachedCertificates.ContainsKey(certPath)
                    && DateTimeOffset.UtcNow < _cachedCertificates[certPath].ValidTill)
                {
                    return _cachedCertificates[certPath].Certificate;
                }

                X509Certificate2 cert = null;
                try
                {
                    cert = await LoadCertificateFromKeyVaultAsync(keyVaultEndpoint, certificateName);
                }
                catch (Exception ex) when (_cachedCertificates.ContainsKey(certPath))
                {
                    _logger.Error(ex, "Cannot refresh the certificate with path {certPath}. Stick with the old one.", certPath);
                    _cachedCertificates[certPath].ValidTill = DateTimeOffset.UtcNow + _certificateCacheTTL;
                    return _cachedCertificates[certPath].Certificate;
                }

                _cachedCertificates[certPath] = new CertificateCacheItem()
                {
                    Certificate = cert,
                    ValidTill = DateTimeOffset.UtcNow + _certificateCacheTTL,
                };

                return cert;
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
                    _logger.Information("Start loading certificate with name {CertificateName} from key vault with endpoint {KeyVaultEndpoint} ...", certificateName, keyVaultEndpoint.AbsoluteUri);
                    var secretBundle = await _kvClient.GetSecretAsync(keyVaultEndpoint.AbsoluteUri, certificateName);

                    _logger.Information(
                        "Loaded the certificate with secretIdentifier: {secretIdentifier}. certificateCreationTime: {certCreated} certificateExpireTime: {certExpire}",
                        secretBundle.SecretIdentifier.Identifier,
                        secretBundle.Attributes.Created.Value.ToZuluString(),
                        secretBundle.Attributes.Expires.Value.ToZuluString());

                    var privateKeyBytes = Convert.FromBase64String(secretBundle.Value);
                    string password = null;

                    // We need to specify the certificate as Exportable explicitly.
                    // On Linux keys are always exportable, but on Windows and macOS they aren't always.
                    return new X509Certificate2(privateKeyBytes, password, X509KeyStorageFlags.Exportable);
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

    internal class CertificateCacheItem
    {
        public X509Certificate2 Certificate { get; set; }

        public DateTimeOffset ValidTill { get; set; }
    }
}
