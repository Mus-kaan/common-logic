//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public partial class InfrastructureV2
    {
        private const string SSHUserNameSecretName = "SSHUserName";
        private const string SSHPublicKeySecretName = "SSHPublicKey";
        private const string SSHPrivateKeySecretName = "SSHPrivateKey";
        private const string OneCertIssuerName = "one-cert-issuer";
        private const string OneCertProvider = "OneCert";

        private readonly ILiftrAzureFactory _azureClientFactory;
        private readonly KeyVaultClient _kvClient;
        private readonly ILogger _logger;

        public InfrastructureV2(ILiftrAzureFactory azureClientFactory, KeyVaultClient kvClient, ILogger logger)
        {
            _azureClientFactory = azureClientFactory ?? throw new ArgumentNullException(nameof(azureClientFactory));
            _kvClient = kvClient ?? throw new ArgumentNullException(nameof(kvClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IVault> GetKeyVaultAsync(string baseName, NamingContext namingContext, bool enableVNet)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            var rgName = namingContext.ResourceGroupName(baseName);
            var kvName = namingContext.KeyVaultName(baseName);

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();
            var targetResourceId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.KeyVault/vaults/{kvName}";
            var kv = await liftrAzure.GetKeyVaultByIdAsync(targetResourceId);

            if (enableVNet)
            {
                var currentPublicIP = await MetadataHelper.GetPublicIPAddressAsync();
                _logger.Information("Restrict VNet access to public IP: {currentPublicIP}", currentPublicIP);
                var vnet = await liftrAzure.GetVNetAsync(rgName, namingContext.NetworkName(baseName));
                await liftrAzure.WithKeyVaultAccessFromNetworkAsync(kv, currentPublicIP, null);
            }

            return kv;
        }

        public async Task<IRegistry> GetACRAsync(string baseName, NamingContext namingContext)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            try
            {
                var rgName = namingContext.ResourceGroupName(baseName);
                var acrName = namingContext.ACRName(baseName);

                var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

                var acr = await liftrAzure.GetACRAsync(rgName, acrName);

                return acr;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(GetACRAsync)} failed.");
                throw;
            }
        }

        private async Task CreateCertificatesAsync(
            KeyVaultConcierge kvValet,
            Dictionary<string, string> certificates,
            NamingContext namingContext,
            string domainName)
        {
            _logger.Information("Checking SSL certificate in Key Vault with name {certName} ...", CertificateName.DefaultSSL);
            var hostName = $"{namingContext.Location.ShortName()}.{domainName}";
            var sslCert = new CertificateOptions()
            {
                CertificateName = CertificateName.DefaultSSL,
                SubjectName = hostName,
                SubjectAlternativeNames = new List<string>()
                                    {
                                        hostName,
                                        $"*.{hostName}",
                                        domainName,
                                        $"*.{domainName}",
                                    },
            };
            await kvValet.SetCertificateIssuerAsync(OneCertIssuerName, OneCertProvider);
            await kvValet.CreateCertificateIfNotExistAsync(sslCert.CertificateName, OneCertIssuerName, sslCert.SubjectName, sslCert.SubjectAlternativeNames, namingContext.Tags);

            foreach (var cert in certificates)
            {
                var certName = cert.Key;
                var certSubject = cert.Value;
                if (cert.Key.OrdinalEquals(CertificateName.DefaultSSL))
                {
                    continue;
                }

                _logger.Information("Checking OneCert certificate in Key Vault with name '{certName}' and subject '{certSubject}'...", certName, certSubject);
                var certOptions = new CertificateOptions()
                {
                    CertificateName = certName,
                    SubjectName = certSubject,
                    SubjectAlternativeNames = new List<string>() { certSubject },
                };

                await kvValet.SetCertificateIssuerAsync(OneCertIssuerName, OneCertProvider);
                await kvValet.CreateCertificateIfNotExistAsync(certOptions.CertificateName, OneCertIssuerName, certOptions.SubjectName, certOptions.SubjectAlternativeNames, namingContext.Tags);
            }
        }

        private static readonly HashSet<string> s_secretsAvoidCopy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AKSSPClientSecret",
            "SSHPrivateKey",
            "SSHPublicKey",
            "SSHUserName",
            "ibizaStorageConnectionString",
            "thanos-api",
        };
    }
}
