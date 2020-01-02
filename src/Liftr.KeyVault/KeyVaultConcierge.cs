//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.KeyVault
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "<Pending>")]
    public sealed class KeyVaultConcierge : IDisposable
    {
        private readonly bool _needDispose = false;
        private readonly string _vaultBaseUrl;
        private readonly KeyVaultClient _keyVaultClient;
        private readonly Serilog.ILogger _logger;

        public KeyVaultConcierge(string vaultBaseUrl, string clientId, string clientSecret, Serilog.ILogger logger)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new ArgumentNullException(nameof(clientSecret));
            }

            _vaultBaseUrl = vaultBaseUrl;
            _keyVaultClient = KeyVaultClientFactory.FromClientIdAndSecret(clientId, clientSecret);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _needDispose = true;
        }

        public KeyVaultConcierge(string vaultBaseUrl, KeyVaultClient kvClient, Serilog.ILogger logger)
        {
            if (string.IsNullOrEmpty(vaultBaseUrl))
            {
                throw new ArgumentNullException(nameof(vaultBaseUrl));
            }

            _vaultBaseUrl = vaultBaseUrl;
            _keyVaultClient = kvClient ?? throw new ArgumentNullException(nameof(kvClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "By Design.")]
        public async Task<bool> ContainsSecretAsync(string secretName)
        {
            try
            {
                var result = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl, secretName);
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<SecretBundle> GetSecretAsync(string secretName)
        {
            _logger.Information("Start getting secret with name: {@secretName} ...", secretName);
            var result = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl, secretName);
            _logger.Information("Finished getting secret with name: {@secretName}.", secretName);
            return result;
        }

        public async Task<SecretBundle> SetSecretAsync(string secretName, string value, IDictionary<string, string> tags = null)
        {
            _logger.Information("Start setting secret with name: {@secretName} ...", secretName);
            var result = await _keyVaultClient.SetSecretAsync(_vaultBaseUrl, secretName, value, tags);
            _logger.Information("Finished setting secret with name: {@secretName}.", secretName);
            return result;
        }

        public async Task<IEnumerable<SecretItem>> ListSecretsAsync(string prefix = null)
        {
            List<SecretItem> result = new List<SecretItem>();
            _logger.Information("Start listing secrets with prefix: {@prefix} ...", prefix);
            var secrets = await _keyVaultClient.GetSecretsAsync(_vaultBaseUrl);
            foreach (var secret in secrets)
            {
                if (string.IsNullOrEmpty(prefix) || secret.Identifier.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(secret);
                }
            }

            _logger.Information("Finished listing secrets with name: {@prefix}.", prefix);
            return result;
        }

        public async Task<IssuerBundle> GetCertificateIssuerAsync(string issuerName)
        {
            _logger.Information("Start getting issuer with name: {@issuerName} ...", issuerName);
            var issuer = await _keyVaultClient.GetCertificateIssuerAsync(_vaultBaseUrl, issuerName);
            _logger.Information("Finished getting issuer with name: {@issuerName} .", issuerName);
            return issuer;
        }

        public async Task<IssuerBundle> SetCertificateIssuerAsync(string issuerName, string provider)
        {
            _logger.Information("Start getting issuer with name: {issuerName}, provider: {provider} ...", issuerName, provider);
            var issuer = await _keyVaultClient.SetCertificateIssuerAsync(_vaultBaseUrl, issuerName, provider);
            _logger.Information("Finished getting issuer with name: {@issuerName} .", issuerName);
            return issuer;
        }

        public async Task<CertificateOperation> CreateCertificateAsync(string certName, string issuerName, string certificateSubject, IList<string> subjectAlternativeNames, IDictionary<string, string> tags = null)
        {
            if (string.IsNullOrEmpty(certificateSubject))
            {
                throw new ArgumentNullException(nameof(certificateSubject));
            }

            CertificatePolicy certPolicy = new CertificatePolicy()
            {
                IssuerParameters = new IssuerParameters
                {
                    Name = issuerName,
                },
                KeyProperties = new KeyProperties
                {
                    Exportable = true,
                    KeySize = 2048,
                    KeyType = "RSA",
                    ReuseKey = false,
                },
                SecretProperties = new SecretProperties
                {
                    ContentType = CertificateContentType.Pfx,
                },
                X509CertificateProperties = new X509CertificateProperties
                {
                    Subject = $"CN={certificateSubject}",
                    SubjectAlternativeNames = new SubjectAlternativeNames(emails: null, dnsNames: subjectAlternativeNames),
                    ValidityInMonths = 12,
                },
                LifetimeActions = new List<LifetimeAction>()
                {
                    new LifetimeAction(
                        new Trigger(daysBeforeExpiry: 290), // ECR require certificate to be auto-renewed in less than 90 days.
                        new Azure.KeyVault.Models.Action(ActionType.AutoRenew)),
                },
            };

            var certificateAttributes = new CertificateAttributes
            {
                Enabled = true,
            };

            _logger.Information("Start creating certificate with name {@certificateName}, subject name {certSubjectName} and policy: {@certPolicy} ...", certName, certPolicy.X509CertificateProperties.Subject, certPolicy);
            var certOperation = await _keyVaultClient.CreateCertificateAsync(_vaultBaseUrl, certName, certPolicy, certificateAttributes, tags);

            while (certOperation.Status.OrdinalEquals("InProgress"))
            {
                await Task.Delay(5000);
                certOperation = await _keyVaultClient.GetCertificateOperationAsync(_vaultBaseUrl, certName);
            }

            _logger.Information("Finished cert cration with name {@certificateName}, subject name {certSubjectName}. Operation result: {@certOperation}", certName, certPolicy.X509CertificateProperties.Subject, certOperation);

            if (!certOperation.Status.OrdinalEquals("Completed"))
            {
                throw new KeyVaultErrorException("Failed to create certificate. " + certOperation.Status + certOperation.StatusDetails);
            }

            return certOperation;
        }

        /// <summary>
        /// Download the pfx part of the certificate.
        /// </summary>
        /// <param name="certName">Name of the cert.</param>
        /// <returns>Value is base64 encoded pfx data</returns>
        public async Task<SecretBundle> DownloadCertAsync(string certName)
        {
            _logger.Information("Start getting certificate with name {@certificateName} ...", certName);
            SecretBundle secret = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl, certName);
            _logger.Information("Finished getting certificate with name {@certificateName} .", certName);
            return secret;
        }

        public void Dispose()
        {
            if (_needDispose)
            {
                _keyVaultClient.Dispose();
            }
        }
    }
}
