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
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public partial class InfrastructureV2
    {
        private const string SSHUserNameSecretName = "SSHUserName";
        private const string SSHPasswordSecretName = "SSHPassword";
        private const string SSHPublicKeySecretName = "SSHPublicKey";
        private const string SSHPrivateKeySecretName = "SSHPrivateKey";
        private const string OneCertPublicIssuer = "one-cert-public-issuer";
        private const string OneCertPrivateIssuer = "one-cert-private-issuer";
        private const string OneCertPublicProvider = "OneCertV2-PublicCA";
        private const string OneCertPrivateProvider = "OneCertV2-PrivateCA";

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

        private async Task<(RPAssetOptions, DataAssetOptions)> AddKeyVaultSecretsAsync(
            NamingContext namingContext,
            IVault keyVault,
            string secretPrefix,
            IStorageAccount regionalStorageAccount,
            string cosmosDBActiveKeyName,
            ICosmosDBAccount cosmosDB,
            string globalStorageResourceId,
            string globalKeyVaultResourceId,
            IIdentity msi,
            IStorageAccount acisStorageAccount,
            string globalCosmosDBResourceId,
            IEnumerable<string> dataPlaneSubscriptions)
        {
            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            using (var regionalKVValet = new KeyVaultConcierge(keyVault.VaultUri, _kvClient, _logger))
            {
                var rpAssets = new RPAssetOptions()
                {
                    StorageAccountName = regionalStorageAccount.Name,
                };
                var dataAssets = new DataAssetOptions()
                {
                    StorageAccountName = regionalStorageAccount.Name,
                };

                if (acisStorageAccount != null)
                {
                    rpAssets.ACISStorageAccountName = acisStorageAccount.Name;
                    rpAssets.ACISStorageConnectionString = await acisStorageAccount.GetPrimaryConnectionStringAsync();

                    // TODO: add rotation
                    dataAssets.ACISStorageAccountName = acisStorageAccount.Name;
                    dataAssets.ACISStorageConnectionString = rpAssets.ACISStorageConnectionString;
                }

                if (cosmosDB != null)
                {
                    _logger.Information($"Cosmos DB '{cosmosDB.Id}' provisioning state: {cosmosDB.Inner.ProvisioningState}");
                    rpAssets.ActiveKeyName = cosmosDBActiveKeyName;

                    try
                    {
                        var dbConnectionStrings = await cosmosDB.ListConnectionStringsAsync();
                        rpAssets.CosmosDBConnectionStrings = dbConnectionStrings.ConnectionStrings.Select(c => new CosmosDBConnectionString()
                        {
                            ConnectionString = c.ConnectionString,
                            Description = c.Description,
                        });

                        // TODO: add rotation
                        dataAssets.RegionalDBConnectionString = await cosmosDB.GetPrimaryConnectionStringAsync();
                        dataAssets.RegionalDBReadonlyConnectionString = await cosmosDB.GetPrimaryReadOnlyConnectionStringAsync();
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
                    _logger.Information($"Cosmos DB '{globalCosmosDB.Id}' provisioning state: {globalCosmosDB.Inner.ProvisioningState}");
                    rpAssets.ActiveKeyName = cosmosDBActiveKeyName;

                    try
                    {
                        var dbConnectionStrings = await globalCosmosDB.ListConnectionStringsAsync();
                        rpAssets.GlobalCosmosDBConnectionStrings = dbConnectionStrings.ConnectionStrings.Select(c => new CosmosDBConnectionString()
                        {
                            ConnectionString = c.ConnectionString,
                            Description = c.Description,
                        });

                        // TODO: add rotation
                        dataAssets.GlobalDBConnectionString = await globalCosmosDB.GetPrimaryConnectionStringAsync();
                        dataAssets.GlobalDBReadonlyConnectionString = await globalCosmosDB.GetPrimaryReadOnlyConnectionStringAsync();
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

                    rpAssets.GlobalStorageAccountName = gblStor.Name;
                    dataAssets.GlobalStorageAccountName = gblStor.Name;
                }

                if (dataPlaneSubscriptions != null)
                {
                    rpAssets.DataPlaneSubscriptions = dataPlaneSubscriptions.Select(sub => new DataPlaneSubscriptionInfo() { SubscriptionId = sub });
                    dataAssets.DataPlaneSubscriptions = dataPlaneSubscriptions.Select(sub => new DataPlaneSubscriptionInfo() { SubscriptionId = sub });
                }

                _logger.Information("Puting the RPAssetOptions in the key vault ...");
                await regionalKVValet.SetSecretAsync($"{secretPrefix}-{nameof(RPAssetOptions)}", rpAssets.ToJson(), namingContext.Tags);

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

                return (rpAssets, dataAssets);
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
            await kvValet.SetCertificateIssuerAsync(OneCertPublicIssuer, OneCertPublicProvider);
            await kvValet.CreateCertificateIfNotExistAsync(sslCert.CertificateName, OneCertPublicIssuer, sslCert.SubjectName, sslSubjectNames, certificateTags);

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

                await kvValet.SetCertificateIssuerAsync(OneCertPrivateIssuer, OneCertPrivateProvider);
                await kvValet.CreateCertificateIfNotExistAsync(certOptions.CertificateName, OneCertPrivateIssuer, certOptions.SubjectName, certOptions.SubjectAlternativeNames, certificateTags);
            }
        }

        private async Task CreateKeyVaultCertificateAsync(
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

            await kvValet.SetCertificateIssuerAsync(OneCertPrivateIssuer, OneCertPrivateProvider);
            await kvValet.CreateCertificateIfNotExistAsync(certOptions.CertificateName, OneCertPrivateIssuer, certOptions.SubjectName, certOptions.SubjectAlternativeNames, certificateTags);
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
