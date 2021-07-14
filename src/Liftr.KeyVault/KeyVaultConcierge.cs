//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.KeyVault
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "<Pending>")]
    public sealed class KeyVaultConcierge : IDisposable
    {
        private readonly bool _needDispose = false;
        private readonly string _vaultBaseUrl;
        private readonly IKeyVaultClient _keyVaultClient;
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
            VaultUri = new Uri(vaultBaseUrl);
            _keyVaultClient = KeyVaultClientFactory.FromClientIdAndSecret(clientId, clientSecret);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _needDispose = true;
        }

        public KeyVaultConcierge(string vaultBaseUrl, IKeyVaultClient kvClient, Serilog.ILogger logger)
        {
            if (string.IsNullOrEmpty(vaultBaseUrl))
            {
                throw new ArgumentNullException(nameof(vaultBaseUrl));
            }

            _vaultBaseUrl = vaultBaseUrl;
            VaultUri = new Uri(vaultBaseUrl);
            _keyVaultClient = kvClient ?? throw new ArgumentNullException(nameof(kvClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Uri VaultUri { get; }

        public async Task<bool> ContainsSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl, secretName, cancellationToken);
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<SecretBundle> GetSecretAsync(
            string secretName,
            bool noThrowNotFound = false,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Start getting secret with name '{secretName}' in vault '{vaultBaseUrl}' ...", secretName, _vaultBaseUrl);
            try
            {
                var result = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl, secretName, cancellationToken);
                _logger.Information("Finished getting secret with name: {secretName}.", secretName);
                return result;
            }
            catch (KeyVaultErrorException ex) when (noThrowNotFound && ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<SecretBundle> SetSecretAsync(
            string secretName,
            string value,
            IDictionary<string, string> tags = null,
            bool setOnlyWhenDifferent = true,
            CancellationToken cancellationToken = default)
        {
            if (setOnlyWhenDifferent)
            {
                var existing = await GetSecretAsync(secretName, noThrowNotFound: true, cancellationToken: cancellationToken);
                if (existing != null && existing.Value.StrictEquals(value))
                {
                    _logger.Information("Didn't overwrite the current secret version since the new value is the same.");
                    return existing;
                }
            }

            _logger.Information("Start setting secret with name '{secretName}' in vault '{vaultBaseUrl}' ...", secretName, _vaultBaseUrl);
            var result = await _keyVaultClient.SetSecretAsync(_vaultBaseUrl, secretName, value, tags, cancellationToken: cancellationToken);
            _logger.Information("Finished setting secret with name: {secretName}.", secretName);
            return result;
        }

        public async Task<IEnumerable<SecretItem>> ListSecretsAsync(string prefix = null, CancellationToken cancellationToken = default)
        {
            List<SecretItem> result = new List<SecretItem>();
            _logger.Information("Start listing secrets with prefix '{prefix}' in vault '{vaultBaseUrl}' ...", prefix, _vaultBaseUrl);
            var secrets = await _keyVaultClient.GetSecretsAsync(_vaultBaseUrl, cancellationToken: cancellationToken);
            foreach (var secret in secrets)
            {
                if (string.IsNullOrEmpty(prefix) || secret.Identifier.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(secret);
                }
            }

            _logger.Information("Listed {secretCount} secrets with name: {prefix}.", result.Count, prefix);
            return result;
        }

        public async Task<IssuerBundle> GetCertificateIssuerAsync(string issuerName, CancellationToken cancellationToken = default)
        {
            _logger.Information("Start getting issuer with name '{issuerName}' in vault '{vaultBaseUrl}' ...", issuerName, _vaultBaseUrl);
            var issuer = await _keyVaultClient.GetCertificateIssuerAsync(_vaultBaseUrl, issuerName, cancellationToken);
            _logger.Information("Finished getting issuer with name: {issuerName} .", issuerName);
            return issuer;
        }

        public async Task<IssuerBundle> SetCertificateIssuerAsync(string issuerName, string provider, CancellationToken cancellationToken = default)
        {
            _logger.Information("Start getting issuer with name: '{issuerName}' provider '{provider}' in vault '{vaultBaseUrl}' ...", issuerName, provider, _vaultBaseUrl);
            var issuer = await _keyVaultClient.SetCertificateIssuerAsync(_vaultBaseUrl, issuerName, provider, cancellationToken: cancellationToken);
            _logger.Information("Finished getting issuer with name: {issuerName} .", issuerName);
            return issuer;
        }

        public async Task<CertificateOperation> CreateCertificateIfNotExistAsync(
            string certName,
            string issuerName,
            string certificateSubject,
            IList<string> subjectAlternativeNames,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var existingCert = await GetCertAsync(certName, cancellationToken);
                if (existingCert != null)
                {
                    var privateKeyBytes = Convert.FromBase64String(existingCert.Value);
                    using var certificate = new X509Certificate2(privateKeyBytes);
                    if (certificate.Subject.OrdinalStartsWith($"CN={certificateSubject}"))
                    {
                        _logger.Information("There already exist a certificate with name {certificateName} in vault '{vaultBaseUrl}' with the same subject {subjectName}. Skip creating a new one.", certName, _vaultBaseUrl, certificateSubject);
                        return null;
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Warning(ex, "Get existing certificate failed. Probably the existing one is in an invalid state. Try create a new one.");
            }

            return await CreateCertificateAsync(certName, issuerName, certificateSubject, subjectAlternativeNames, tags, cancellationToken);
        }

        public async Task<CertificateOperation> CreateCertificateAsync(
            string certName,
            string issuerName,
            string certificateSubject,
            IList<string> subjectAlternativeNames,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(certificateSubject))
            {
                throw new ArgumentNullException(nameof(certificateSubject));
            }

            using (var ops = _logger.StartTimedOperation(nameof(CreateCertificateAsync)))
            {
                ops.WithLabel("CertificateName", certName);
                ops.WithLabel(nameof(issuerName), issuerName);
                ops.SetContextProperty(nameof(certificateSubject), certificateSubject);

                try
                {
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

                    _logger.Information("Start creating certificate with name {@certificateName}, subject name {certSubjectName} and policy: {@certPolicy} in vault '{vaultBaseUrl}' ...", certName, certPolicy.X509CertificateProperties.Subject, certPolicy, _vaultBaseUrl);
                    var certOperation = await _keyVaultClient.CreateCertificateAsync(_vaultBaseUrl, certName, certPolicy, certificateAttributes, tags, cancellationToken);

                    while (certOperation.Status.OrdinalEquals("InProgress"))
                    {
                        await Task.Delay(5000);
                        certOperation = await _keyVaultClient.GetCertificateOperationAsync(_vaultBaseUrl, certName, cancellationToken);
                    }

                    _logger.Information("Finished cert cration with name '{certificateName}', subject name '{certSubjectName}'. Operation result: {@certOperation}", certName, certPolicy.X509CertificateProperties.Subject, certOperation);

                    if (!certOperation.Status.OrdinalEquals("Completed"))
                    {
                        _logger.Error("Failed at creating certificate with name '{certificateName}', creation error message: {errorMessage}", certName, certOperation?.Error?.Message);
                        _logger.Error("Certificate creation operation details: {@certOperation}", certOperation);
                        throw new KeyVaultErrorException("Failed to create certificate. " + certOperation?.Error?.Message);
                    }

                    await GetCertificateDetailsAsync(certName, cancellationToken);
                    return certOperation;
                }
                catch (Exception ex)
                {
                    ops.FailOperation(ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Download the pfx part of the certificate.
        /// </summary>
        /// <param name="certName">Name of the cert.</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>Value is base64 encoded pfx data</returns>
        public async Task<SecretBundle> GetCertAsync(string certName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Information("Start getting certificate with name {certificateName} in vault '{vaultBaseUrl}' ...", certName, _vaultBaseUrl);
                SecretBundle secret = await _keyVaultClient.GetSecretAsync(_vaultBaseUrl, certName, cancellationToken);
                await GetCertificateDetailsAsync(certName, cancellationToken);
                return secret;
            }
            catch (KeyVaultErrorException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Get details of the certificate.
        /// </summary>
        /// <param name="certName">Name of the cert.</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>CertificateBundle object</returns>
        public async Task<CertificateBundle> GetCertificateDetailsAsync(string certName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Information("Fetching certificate details with name {certificateName} in vault '{vaultBaseUrl}' ...", certName, _vaultBaseUrl);
                CertificateBundle certDetails = await _keyVaultClient.GetCertificateAsync(_vaultBaseUrl, certName, cancellationToken);
                var thumbprint = StringExtensions.GetStringFromBytes(certDetails.X509Thumbprint);

                _logger.Information("Finished getting certificate details with name {certificateName}, Thumbprint: {thumbprint} and Id: {certId}...", certName, thumbprint, certDetails.Id);
                return certDetails;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
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
