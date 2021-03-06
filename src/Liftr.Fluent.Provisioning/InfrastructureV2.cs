//-----------------------------------------------------------------------------
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public partial class InfrastructureV2
    {
        public const string SSHUserNameSecretName = "SSHUserName";
        public const string SSHPasswordSecretName = "SSHPassword";
        public const string SSHPublicKeySecretName = "SSHPublicKey";
        public const string SSHPrivateKeySecretName = "SSHPrivateKey";
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

        public string GetLiftrCommonACREndpoint(NamingContext namingContext)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            try
            {
                var acrName = namingContext.LiftrCommonACRName();
                var acrEndpoint = NamingContext.LiftrCommonACREndpoint(acrName);

                return acrEndpoint;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(GetLiftrCommonACREndpoint)} failed.");
                throw;
            }
        }

        private async Task<DataAssetOptions> AddKeyVaultSecretsAsync(
            NamingContext namingContext,
            IVault keyVault,
            string secretPrefix,
            IStorageAccount regionalStorageAccount,
            ICosmosDBAccount cosmosDB,
            string globalStorageResourceId,
            string globalKeyVaultResourceId,
            IIdentity msi,
            IStorageAccount acisStorageAccount,
            string globalCosmosDBResourceId,
            IEnumerable<string> dataPlaneSubscriptions,
            IEnumerable<string> outboundIPList)
        {
            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            using (var regionalKVValet = new KeyVaultConcierge(keyVault.VaultUri, _kvClient, _logger))
            {
                var dataAssets = new DataAssetOptions()
                {
                    StorageAccountName = regionalStorageAccount.Name,
                };

                if (acisStorageAccount != null)
                {
                    var storageAccountCredentialManager = new StorageAccountCredentialLifeCycleManager(acisStorageAccount, new SystemTimeSource(), _logger);
                    dataAssets.ACISStorageAccountName = acisStorageAccount.Name;
                    dataAssets.ACISStorageConnectionString = await storageAccountCredentialManager.GetActiveConnectionStringAsync();
                }

                if (cosmosDB != null)
                {
                    _logger.Information($"Cosmos DB '{cosmosDB.Id}' provisioning state: {cosmosDB.Inner.ProvisioningState}");

                    try
                    {
                        var regionalCosmosDBCredentialManager = new CosmosDBCredentialLifeCycleManager(cosmosDB, new SystemTimeSource(), _logger);
                        dataAssets.RegionalDBConnectionString = await regionalCosmosDBCredentialManager.GetActiveConnectionStringAsync();
                        dataAssets.RegionalDBReadonlyConnectionString = await regionalCosmosDBCredentialManager.GetActiveConnectionStringAsync(readOnly: true);
                    }
                    catch (Exception dbEx)
                    {
                        var errMsg = $"We cannot get the connection string of '{cosmosDB.Id}'. You can open the cosmos DB in portal and view details. You can open a support ticket for help.";
                        var ex = new InvalidOperationException(errMsg, dbEx);
                        _logger.Error(dbEx, errMsg);
                        throw ex;
                    }
                }

                ICosmosDBAccount globalCosmosDB = null;
                if (!string.IsNullOrEmpty(globalCosmosDBResourceId))
                {
                    globalCosmosDB = await liftrAzure.GetCosmosDBAsync(globalCosmosDBResourceId);
                }

                if (globalCosmosDB != null)
                {
                    _logger.Information($"Global Cosmos DB '{globalCosmosDB.Id}' provisioning state: {globalCosmosDB.Inner.ProvisioningState}");

                    try
                    {
                        var globalCosmosDBCredentialManager = new CosmosDBCredentialLifeCycleManager(globalCosmosDB, new SystemTimeSource(), _logger);
                        dataAssets.GlobalDBConnectionString = await globalCosmosDBCredentialManager.GetActiveConnectionStringAsync();
                        dataAssets.GlobalDBReadonlyConnectionString = await globalCosmosDBCredentialManager.GetActiveConnectionStringAsync(readOnly: true);
                    }
                    catch (Exception dbEx)
                    {
                        var errMsg = $"We cannot get the connection string of '{globalCosmosDB.Id}'. You can open the cosmos DB in portal and view details. You can open a support ticket for help.";
                        var ex = new InvalidOperationException(errMsg, dbEx);
                        _logger.Error(dbEx, errMsg);
                        throw ex;
                    }
                }

                if (!string.IsNullOrEmpty(globalStorageResourceId))
                {
                    var storId = new ResourceId(globalStorageResourceId);
                    var gblStor = await liftrAzure.GetStorageAccountAsync(storId.ResourceGroup, storId.ResourceName);
                    if (gblStor == null)
                    {
                        throw new InvalidOperationException("Cannot find the global storage account with Id: " + globalStorageResourceId);
                    }

                    dataAssets.GlobalStorageAccountName = gblStor.Name;
                }

                if (dataPlaneSubscriptions != null)
                {
                    dataAssets.DataPlaneSubscriptions = dataPlaneSubscriptions.Select(sub => new DataPlaneSubscriptionInfo() { SubscriptionId = sub });
                }

                if (outboundIPList?.Any() == true)
                {
                    dataAssets.OutboundIPList = outboundIPList;
                }

                _logger.Information("Puting the DataAssetOptions in the key vault ...");
                await regionalKVValet.SetSecretAsync($"{secretPrefix}-{nameof(DataAssetOptions)}", dataAssets.ToJson(), namingContext.Tags);

                _logger.Information($"Puting the key vault Uri '{keyVault.VaultUri}' in the key vault secret ...");
                await regionalKVValet.SetSecretAsync("vaultUri", keyVault.VaultUri, namingContext.Tags);

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

                return dataAssets;
            }
        }

        private async Task CreateKeyVaultCertificatesAsync(
            KeyVaultConcierge kvValet,
            Dictionary<string, string> certificates,
            IList<string> sslSubjectNames,
            IDictionary<string, string> certificateTags)
        {
            _logger.Information("Checking SSL certificate in Key Vault with name {certName} ...", CertificateName.DefaultSSL);
            var sslCert = new CertificateOptions()
            {
                CertificateName = CertificateName.DefaultSSL,
                SubjectName = sslSubjectNames.First(),
            };
            await kvValet.SetCertificateIssuerAsync(KeyVaultConcierge.OneCertPublicIssuer, KeyVaultConcierge.OneCertPublicProvider);
            await kvValet.CreateCertificateIfNotExistAsync(sslCert.CertificateName, KeyVaultConcierge.OneCertPublicIssuer, sslCert.SubjectName, sslSubjectNames, certificateTags);
            await kvValet.CreatePrivateIssuerCertificatesAsync(certificates, certificateTags);
        }

        private async Task CreateCrdentialSharingCertificateAsync(
           KeyVaultConcierge kvValet,
           string certName,
           string certSubject,
           IDictionary<string, string> certificateTags = null)
        {
            _logger.Information("Checking OneCert certificate in Key Vault with name '{certName}' and subject '{certSubject}'...", certName, certSubject);
            var certOptions = new CertificateOptions()
            {
                CertificateName = certName,
                SubjectName = certSubject,
                SubjectAlternativeNames = new List<string>() { certSubject },
            };

            await kvValet.SetCertificateIssuerAsync(KeyVaultConcierge.OneCertPrivateIssuer, KeyVaultConcierge.OneCertPrivateProvider);
            await kvValet.CreateCertificateIfNotExistAsync(certOptions.CertificateName, KeyVaultConcierge.OneCertPrivateIssuer, certOptions.SubjectName, certOptions.SubjectAlternativeNames, certificateTags);
        }

        private static readonly HashSet<string> s_secretsAvoidCopy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AKSSPClientSecret",
            "SSHPrivateKey",
            "SSHPublicKey",
            "SSHUserName",
            "SSHPassword",
            "ibizaStorageConnectionString",
            "thanos-api",
            "GlobalDataEncryptionKey",
            "PartnerCredSharingAppCert",
        };
    }
}
