//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.BatchAI.Fluent.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public sealed class KeyVaultConcierge : IDisposable
    {
        private readonly string _vaultBaseUrl;
        private readonly KeyVaultClient _keyVaultClient;
        private readonly Serilog.ILogger _logger;

#pragma warning disable CA1054 // Uri parameters should not be strings
        public KeyVaultConcierge(string vaultBaseUrl, string clientId, string clientSecret, Serilog.ILogger logger)
#pragma warning restore CA1054 // Uri parameters should not be strings
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
            _keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) =>
                {
                    var authContext = new AuthenticationContext(authority, TokenCache.DefaultShared);
                    var result = await authContext.AcquireTokenAsync(resource, new ClientCredential(clientId, clientSecret));
                    return result.AccessToken;
                }), new HttpClient());
            _logger = logger;
        }

        public async Task<SecretBundle> SetSecretAsync(string secretName, string value, IDictionary<string, string> tags = null)
        {
            _logger.Information("Start setting secret with name: {@secretName} ...", secretName);
            var result = await _keyVaultClient.SetSecretAsync(_vaultBaseUrl, secretName, value, tags);
            _logger.Information("Finished setting secret with name: {@secretName}.", secretName);
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
            _logger.Information("Start getting issuer with name: {@issuerName}, provider: {@provider} ...", issuerName, provider);
            var issuer = await _keyVaultClient.SetCertificateIssuerAsync(_vaultBaseUrl, issuerName, provider);
            _logger.Information("Finished getting issuer with name: {@issuerName} .", issuerName);
            return issuer;
        }

        public async Task<CertificateOperation> CreateCertificateAsync(string certName, string issuerName, string certificateSubject, IList<string> subjectAlternativeNames, IDictionary<string, string> tags = null)
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
                        new Trigger(lifetimePercentage: 50),
                        new Azure.KeyVault.Models.Action(ActionType.EmailContacts)),
                },
            };

            var certificateAttributes = new CertificateAttributes
            {
                Enabled = true,
            };

            _logger.Information("Start creating certificate with name {@certificateName} and policy: {@certPolicy} ...", certName, certPolicy);
            var certOperation = await _keyVaultClient.CreateCertificateAsync(_vaultBaseUrl, certName, certPolicy, certificateAttributes, tags);

            while (certOperation.Status.OrdinalEquals("InProgress"))
            {
                await Task.Delay(5000);
                certOperation = await _keyVaultClient.GetCertificateOperationAsync(_vaultBaseUrl, certName);
            }

            _logger.Information("Finished cert cration with name {@certificateName}. Operation result: {@certOperation}", certName, certOperation);

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
            _keyVaultClient.Dispose();
        }
    }
}
