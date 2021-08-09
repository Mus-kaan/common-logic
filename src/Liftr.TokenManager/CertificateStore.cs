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
        private readonly Dictionary<string, CertificateCacheItem> _cachedCertificates = new Dictionary<string, CertificateCacheItem>();

        public CertificateStore(IKeyVaultClient kvClient, ILogger logger, TimeSpan? certificateCacheTTL = null)
        {
            _kvClient = kvClient ?? throw new ArgumentNullException(nameof(kvClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CertificateCacheTTL = certificateCacheTTL ?? TimeSpan.FromMinutes(30);
        }

        public TimeSpan CertificateCacheTTL { get; }

        public void Dispose()
        {
            _mu.Dispose();
        }

        /// <summary>
        /// To avoid leak, the X509Certificate2 object needs to be disposed after usage.
        /// However since this class doesn't know when the caller will finish using the cert object, there is no way for it to dispose the object
        /// As a result, CertificateStore only caches the raw bytes internally. It always return a new X509Certificate2 object to caller.
        /// It's the caller's responsibility to dispose the cert object after usage
        /// </summary>
        /// <param name="keyVaultEndpoint"></param>
        /// <param name="certificateName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<X509Certificate2> GetCertificateAsync(Uri keyVaultEndpoint, string certificateName, CancellationToken cancellationToken = default)
        {
            if (keyVaultEndpoint == null)
            {
                throw new ArgumentNullException(nameof(keyVaultEndpoint));
            }

            var certPath = $"{keyVaultEndpoint.AbsoluteUri}/certificates/{certificateName}";

            await _mu.WaitAsync(cancellationToken);
            try
            {
                if (_cachedCertificates.ContainsKey(certPath)
                    && DateTimeOffset.UtcNow < _cachedCertificates[certPath].ValidTill)
                {
                    return CreateCert(_cachedCertificates[certPath].Certificate);
                }

                byte[] cert = null;
                try
                {
                    cert = await LoadCertificateFromKeyVaultAsync(keyVaultEndpoint, certificateName, cancellationToken);
                }
                catch (Exception ex) when (_cachedCertificates.ContainsKey(certPath))
                {
                    _logger.Error(ex, "Cannot refresh the certificate with path {certPath}. Stick with the old one.", certPath);
                    _cachedCertificates[certPath].ValidTill = DateTimeOffset.UtcNow + CertificateCacheTTL;
                    return CreateCert(_cachedCertificates[certPath].Certificate);
                }

                _cachedCertificates[certPath] = new CertificateCacheItem()
                {
                    Certificate = cert,
                    ValidTill = DateTimeOffset.UtcNow + CertificateCacheTTL,
                };

                return CreateCert(cert);
            }
            finally
            {
                _mu.Release();
            }
        }

        private async Task<byte[]> LoadCertificateFromKeyVaultAsync(Uri keyVaultEndpoint, string certificateName, CancellationToken cancellationToken)
        {
            using (var operation = _logger.StartTimedOperation(nameof(LoadCertificateFromKeyVaultAsync)))
            {
                operation.SetContextProperty(nameof(certificateName), certificateName);
                try
                {
                    _logger.Information("Start loading certificate with name {CertificateName} from key vault with endpoint {KeyVaultEndpoint} ...", certificateName, keyVaultEndpoint.AbsoluteUri);
                    var secretBundle = await _kvClient.GetSecretAsync(keyVaultEndpoint.AbsoluteUri, certificateName, cancellationToken);

                    _logger.Information(
                        "Loaded the certificate with secretIdentifier: {secretIdentifier}. certificateCreationTime: {certCreated} certificateExpireTime: {certExpire}",
                        secretBundle.SecretIdentifier.Identifier,
                        secretBundle.Attributes.Created.Value.ToZuluString(),
                        secretBundle.Attributes.Expires.Value.ToZuluString());

                    return Convert.FromBase64String(secretBundle.Value);
                }
                catch (Exception ex)
                {
                    operation.FailOperation();
                    _logger.Fatal(ex, "Failed to load certificate: {certificateName} from keyvault {keyvault}", certificateName, keyVaultEndpoint.AbsoluteUri);
                    throw;
                }
            }
        }

        private static X509Certificate2 CreateCert(byte[] privateBytes)
        {
            // We need to specify the certificate as Exportable explicitly.
            // On Linux keys are always exportable, but on Windows and macOS they aren't always.
            string password = null;
            return new X509Certificate2(privateBytes, password, X509KeyStorageFlags.Exportable);
        }
    }

    internal class CertificateCacheItem
    {
        public byte[] Certificate { get; set; }

        public DateTimeOffset ValidTill { get; set; }
    }
}
