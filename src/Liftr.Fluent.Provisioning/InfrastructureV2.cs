﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Hosting.Contracts;
using Microsoft.Liftr.KeyVault;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private async Task<RPAssetOptions> AddKeyVaultSecretsAsync(
            NamingContext namingContext,
            IVault keyVault,
            string secretPrefix,
            IStorageAccount regionalStorageAccount,
            string cosmosDBActiveKeyName,
            ICosmosDBAccount cosmosDB,
            string globalStorageResourceId,
            string globalKeyVaultResourceId,
            IIdentity msi)
        {
            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            using (var regionalKVValet = new KeyVaultConcierge(keyVault.VaultUri, _kvClient, _logger))
            {
                var rpAssets = new RPAssetOptions()
                {
                    StorageAccountName = regionalStorageAccount.Name,
                };

                if (cosmosDB != null)
                {
                    rpAssets.ActiveKeyName = cosmosDBActiveKeyName;
                    var dbConnectionStrings = await cosmosDB.ListConnectionStringsAsync();
                    rpAssets.CosmosDBConnectionStrings = dbConnectionStrings.ConnectionStrings.Select(c => new CosmosDBConnectionString()
                    {
                        ConnectionString = c.ConnectionString,
                        Description = c.Description,
                    });
                }

                if (!string.IsNullOrEmpty(globalStorageResourceId))
                {
                    var storId = new ResourceId(globalStorageResourceId);
                    var gblStor = await liftrAzure.GetStorageAccountAsync(storId.ResourceGroup, storId.ResourceName);
                    if (gblStor == null)
                    {
                        throw new InvalidOperationException("Cannot find the global storage account with Id: " + globalStorageResourceId);
                    }

                    rpAssets.GlobalStorageAccountName = gblStor.Name;
                }

                _logger.Information("Puting the RPAssetOptions in the key vault ...");
                await regionalKVValet.SetSecretAsync($"{secretPrefix}-{nameof(RPAssetOptions)}", rpAssets.ToJson(), namingContext.Tags);

                var envOptions = new RunningEnvironmentOptions()
                {
                    TenantId = msi.TenantId,
                    SPNObjectId = msi.GetObjectId(),
                };

                _logger.Information($"Puting the {nameof(RunningEnvironmentOptions)} in the key vault ...");
                await regionalKVValet.SetSecretAsync($"{secretPrefix}-{nameof(RunningEnvironmentOptions)}--{nameof(envOptions.TenantId)}", envOptions.TenantId, namingContext.Tags);
                await regionalKVValet.SetSecretAsync($"{secretPrefix}-{nameof(RunningEnvironmentOptions)}--{nameof(envOptions.SPNObjectId)}", envOptions.SPNObjectId, namingContext.Tags);

                // Move the secrets from global key vault to regional key vault.
                var globalKv = await liftrAzure.GetKeyVaultByIdAsync(globalKeyVaultResourceId);
                if (globalKv == null)
                {
                    throw new InvalidOperationException($"Cannot find the global key vault with resource Id '{globalKeyVaultResourceId}'");
                }

                using (var globalKVValet = new KeyVaultConcierge(globalKv.VaultUri, _kvClient, _logger))
                {
                    _logger.Information($"Start copying the secrets from global key vault ...");
                    int cnt = 0;
                    var secretsToCopy = await globalKVValet.ListSecretsAsync();
                    foreach (var secret in secretsToCopy)
                    {
                        if (s_secretsAvoidCopy.Contains(secret.Identifier.Name))
                        {
                            continue;
                        }

                        var secretBundle = await globalKVValet.GetSecretAsync(secret.Identifier.Name);
                        await regionalKVValet.SetSecretAsync(secret.Identifier.Name, secretBundle.Value, secretBundle.Tags);
                        _logger.Information("Copied secert with name: {secretName}", secret.Identifier.Name);
                        cnt++;
                    }

                    _logger.Information("Copied {copiedSecretCount} secrets from central key vault to local key vault.", cnt);
                }

                return rpAssets;
            }
        }

        private async Task CreateKeyVaultCertificatesAsync(
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
