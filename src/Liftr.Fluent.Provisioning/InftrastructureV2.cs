//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Rest.Azure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class InftrastructureV2
    {
        private readonly ILiftrAzureFactory _azureClientFactory;
        private readonly ILogger _logger;

        public InftrastructureV2(ILiftrAzureFactory azureClientFactory, ILogger logger)
        {
            _azureClientFactory = azureClientFactory ?? throw new ArgumentNullException(nameof(azureClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IVault> CreateOrUpdateGlobalRGAsync(string baseName, NamingContext namingContext)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            try
            {
                IVault kv = null;

                var rgName = namingContext.ResourceGroupName(baseName);
                var kvName = namingContext.KeyVaultName(baseName);

                var client = _azureClientFactory.GenerateLiftrAzure();

                _logger.Information("Creating Resource Group ...");
                try
                {
                    var rg = await client.CreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
                    _logger.Information("Created Resource Group with Id {ResourceId}", rg.Id);
                }
                catch (DuplicateNameException ex)
                {
                    _logger.Information("There exist a RG with the same name. Reuse it. {ExceptionDetail}", ex);
                }

                kv = await client.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags);

                await client.GrantSelfKeyVaultAdminAccessAsync(kv);

                return kv;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(CreateOrUpdateGlobalRGAsync)} failed.");
                throw;
            }
        }

        public async Task<(IResourceGroup rg, IIdentity msi, ICosmosDBAccount db, ITrafficManagerProfile tm, IVault kv)> CreateOrUpdateRegionalDataRGAsync(
            string baseName,
            NamingContext namingContext,
            IEnumerable<string> dataPlaneSubscriptions = null,
            int dataPlaneStorageCountPerSubscription = 0)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            if (dataPlaneStorageCountPerSubscription > 0)
            {
                if (dataPlaneSubscriptions == null || !dataPlaneSubscriptions.Any())
                {
                    throw new ArgumentException("data plane Subscriptions cannot be empty.", nameof(dataPlaneSubscriptions));
                }
            }

            ICosmosDBAccount db = null;
            ITrafficManagerProfile tm = null;
            IVault kv = null;

            var rgName = namingContext.ResourceGroupName(baseName);
            var storageName = namingContext.StorageAccountName(baseName);
            var trafficManagerName = namingContext.TrafficManagerName(baseName);
            var kvName = namingContext.KeyVaultName(baseName);
            var cosmosName = namingContext.CosmosDBName(baseName);
            var msiName = namingContext.MSIName(baseName);

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            var rg = await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);

            var stor = await liftrAzure.GetOrCreateStorageAccountAsync(namingContext.Location, rgName, storageName, namingContext.Tags);
            await liftrAzure.DelegateStorageKeyOperationToKeyVaultAsync(stor);

            var msi = await liftrAzure.GetOrCreateMSIAsync(namingContext.Location, rgName, msiName, namingContext.Tags);

            if (dataPlaneSubscriptions != null)
            {
                foreach (var subscrptionId in dataPlaneSubscriptions)
                {
                    try
                    {
                        _logger.Information("Granting the MSI {MSIReourceId} contributor role to the subscription with {subscrptionId} ...", msi.Id, subscrptionId);
                        await liftrAzure.Authenticated.RoleAssignments
                            .Define(SdkContext.RandomGuid())
                            .ForObjectId(msi.GetObjectId())
                            .WithBuiltInRole(BuiltInRole.Contributor)
                            .WithSubscriptionScope(subscrptionId)
                            .CreateAsync();
                    }
                    catch (CloudException ex) when (ex.Message.Contains("The role assignment already exists"))
                    {
                        _logger.Information("There exists the same role assignment for the MSI {MSIReourceId} to the subscription {subscrptionId}.", msi.Id, subscrptionId);
                    }
                }
            }

            _logger.Information("Creating Traffic Manager ...");
            var targetResourceId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.Network/trafficmanagerprofiles/{trafficManagerName}";
            tm = await liftrAzure.GetTrafficManagerAsync(targetResourceId);
            if (tm == null)
            {
                tm = await liftrAzure.CreateTrafficManagerAsync(rgName, trafficManagerName, namingContext.Tags);
                _logger.Information("Created Traffic Manager with Id {ResourceId}", tm.Id);
            }
            else
            {
                _logger.Information("There has an existing Traffic Manager with the same {ResourceId}. Stop creating a new one.", tm.Id);
            }

            kv = await liftrAzure.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags);

            _logger.Information("Start adding access policy for msi to regional kv.");
            await kv.Update()
                .DefineAccessPolicy()
                .ForObjectId(msi.GetObjectId())
                .AllowSecretPermissions(SecretPermissions.List, SecretPermissions.Get)
                .AllowCertificatePermissions(CertificatePermissions.Get, CertificatePermissions.Getissuers, CertificatePermissions.List)
                .Attach()
                .ApplyAsync();
            _logger.Information("Added access policy for msi to regional kv.");

            _logger.Information("Creating CosmosDB ...");
            targetResourceId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosName}";
            db = await liftrAzure.GetCosmosDBAsync(targetResourceId);
            if (db == null)
            {
                (var createdDb, _) = await liftrAzure.CreateCosmosDBAsync(namingContext.Location, rgName, cosmosName, namingContext.Tags);
                db = createdDb;
                _logger.Information("Created CosmosDB with Id {ResourceId}", db.Id);
            }
            else
            {
                _logger.Information("There has an existing CosmosDB with the same {ResourceId}. Stop creating a new one.", db.Id);
            }

            if (dataPlaneStorageCountPerSubscription > 0 && dataPlaneSubscriptions != null)
            {
                foreach (var dpSubscription in dataPlaneSubscriptions)
                {
                    var dataPlaneLiftrAzure = _azureClientFactory.GenerateLiftrAzure(dpSubscription);
                    IResourceGroup dataPlaneStorageRG = await dataPlaneLiftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, namingContext.ResourceGroupName(baseName + "-dp"), namingContext.Tags);
                    await dataPlaneLiftrAzure.DelegateStorageKeyOperationToKeyVaultAsync(dataPlaneStorageRG);

                    var existingStorageAccountCount = (await dataPlaneLiftrAzure.ListStorageAccountAsync(dataPlaneStorageRG.Name)).Count();

                    for (int i = existingStorageAccountCount; i < dataPlaneStorageCountPerSubscription; i++)
                    {
                        var storageAccountName = SdkContext.RandomResourceName(baseName, 24);
                        await dataPlaneLiftrAzure.GetOrCreateStorageAccountAsync(namingContext.Location, dataPlaneStorageRG.Name, storageAccountName, namingContext.Tags);
                    }
                }
            }

            return (rg, msi, db, tm, kv);
        }

        public async Task<IVault> GetKeyVaultAsync(string baseName, NamingContext namingContext)
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
            return kv;
        }

        public async Task<(IVault kv, IIdentity msi, IKubernetesCluster aks)> CreateOrUpdateRegionalComputeRGAsync(
            NamingContext namingContext,
            InfraV2RegionalComputeOptions computeOptions,
            AKSInfo aksInfo,
            KeyVaultClient kvClient,
            CertificateOptions genevaCert,
            CertificateOptions sslCert,
            CertificateOptions firstPartyCert)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            if (computeOptions == null)
            {
                throw new ArgumentNullException(nameof(computeOptions));
            }

            if (aksInfo == null)
            {
                throw new ArgumentNullException(nameof(aksInfo));
            }

            _logger.Information("InfraV2RegionalComputeOptions: {@InfraV2RegionalComputeOptions}", computeOptions);
            _logger.Information("AKSInfo: {@AKSInfo}", aksInfo);
            computeOptions.CheckValues();
            aksInfo.CheckValues();

            _logger.Information("AKS machine type: {AKSMachineType}", aksInfo.AKSMachineTypeStr);
            _logger.Information("AKS machine count: {AKSMachineCount}", aksInfo.AKSMachineCount);

            IVault kv = null;
            IKubernetesCluster aks = null;

            var rgName = namingContext.ResourceGroupName(computeOptions.ComputeBaseName);
            var dataRGName = namingContext.ResourceGroupName(computeOptions.DataBaseName);
            var kvName = namingContext.KeyVaultName(computeOptions.ComputeBaseName);
            var aksName = namingContext.AKSName(computeOptions.ComputeBaseName);

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            var msiName = namingContext.MSIName(computeOptions.DataBaseName);
            var regionalMsi = await liftrAzure.GetMSIAsync(namingContext.ResourceGroupName(computeOptions.DataBaseName), msiName);
            if (regionalMsi == null)
            {
                var ex = new InvalidOperationException("Cannot find regional MSI with resource name: " + msiName);
                _logger.Error("Cannot find regional MSI with resource name: {ResourceName}", msiName);
                throw ex;
            }

            var dbId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{namingContext.ResourceGroupName(computeOptions.DataBaseName)}/providers/Microsoft.DocumentDB/databaseAccounts/{namingContext.CosmosDBName(computeOptions.DataBaseName)}";
            var db = await liftrAzure.GetCosmosDBAsync(dbId);
            if (db == null)
            {
                var ex = new InvalidOperationException("Cannot find cosmos DB with resource Id: " + dbId);
                _logger.Error("Cannot find cosmos DB with resource Id: {ResourceId}", dbId);
                throw ex;
            }

            var storageAccount = await liftrAzure.GetStorageAccountAsync(dataRGName, namingContext.StorageAccountName(computeOptions.DataBaseName));
            if (storageAccount == null)
            {
                var errMsg = $"Cannot find the storage with name {namingContext.StorageAccountName(computeOptions.DataBaseName)} in RG {dataRGName}.";
                var ex = new InvalidOperationException(errMsg);
                _logger.Error(ex, errMsg);
                throw ex;
            }

            var centralKv = await liftrAzure.GetKeyVaultByIdAsync(computeOptions.CentralKeyVaultResourceId);
            if (centralKv == null)
            {
                var ex = new InvalidOperationException("Cannot find central key vault with resource Id: " + computeOptions.CentralKeyVaultResourceId);
                _logger.Error("Cannot find central key vault with resource Id: {ResourceId}", computeOptions.CentralKeyVaultResourceId);
                throw ex;
            }

            using (var centralKVValet = new KeyVaultConcierge(centralKv.VaultUri, kvClient, _logger))
            {
                var aksSPNClientSecret = await centralKVValet.GetSecretAsync(aksInfo.AKSSPNClientSecretName);
                if (aksSPNClientSecret == null)
                {
                    var errorMsg = $"Cannot find the AKS SPN client secret in key vault with secret name: {aksInfo.AKSSPNClientSecretName}";
                    _logger.Error(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                _logger.Information("Creating Resource Group ...");
                try
                {
                    var rg = await liftrAzure.CreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
                    _logger.Information("Created Resource Group with Id {ResourceId}", rg.Id);
                }
                catch (DuplicateNameException ex)
                {
                    _logger.Information("There exist a RG with the same name. Reuse it. {ExceptionDetail}", ex);
                }

                try
                {
                    _logger.Information("Granting the identity binding access for the MSI {MSIResourceId} to the AKS SPN with {AKSobjectId} ...", regionalMsi.Id, aksInfo.AKSSPNObjectId);
                    await liftrAzure.Authenticated.RoleAssignments
                        .Define(SdkContext.RandomGuid())
                        .ForObjectId(aksInfo.AKSSPNObjectId)
                        .WithBuiltInRole(BuiltInRole.Contributor)
                        .WithResourceScope(regionalMsi)
                        .CreateAsync();
                    _logger.Information("Granted the identity binding access for the MSI {MSIResourceId} to the AKS SPN with {AKSobjectId}.", regionalMsi.Id, aksInfo.AKSSPNObjectId);
                }
                catch (CloudException ex) when (ex.Message.Contains("The role assignment already exists"))
                {
                    _logger.Information("There exists the same role assignment for the MSI {MSIResourceId} to the AKS SPN with {AKSobjectId}.", regionalMsi.Id, aksInfo.AKSSPNObjectId);
                }

                kv = await liftrAzure.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags);

                await liftrAzure.GrantSelfKeyVaultAdminAccessAsync(kv);
                await liftrAzure.GrantQueueContributorAsync(storageAccount, regionalMsi);

                _logger.Information("Start adding access policy for msi to local kv.");
                await kv.Update()
                    .DefineAccessPolicy()
                    .ForObjectId(regionalMsi.GetObjectId())
                    .AllowSecretPermissions(SecretPermissions.List, SecretPermissions.Get)
                    .AllowCertificatePermissions(CertificatePermissions.Get, CertificatePermissions.Getissuers, CertificatePermissions.List)
                    .Attach()
                    .ApplyAsync();
                _logger.Information("Added access policy for msi to local kv.");

                using (var valet = new KeyVaultConcierge(kv.VaultUri, kvClient, _logger))
                {
                    _logger.Information($"Start copying the secrets in central key vault with prefix {computeOptions.CopyKVSecretsWithPrefix}...");
                    int cnt = 0;
                    var toCopy = await centralKVValet.ListSecretsAsync(computeOptions.CopyKVSecretsWithPrefix);
                    foreach (var secret in toCopy)
                    {
                        var secretBundle = await centralKVValet.GetSecretAsync(secret.Identifier.Name);
                        await valet.SetSecretAsync(secret.Identifier.Name, secretBundle.Value, namingContext.Tags);
                        cnt++;
                    }

                    _logger.Information($"Copied {cnt} secrets from central key vault to local key vault.");

                    var rpAssets = new RPAssetOptions();
                    var dbConnectionStrings = await db.ListConnectionStringsAsync();
                    rpAssets.CosmosDBConnectionString = dbConnectionStrings.ConnectionStrings[0].ConnectionString;
                    rpAssets.StorageAccountName = storageAccount.Name;

                    if (computeOptions.DataPlaneSubscriptions != null)
                    {
                        var dpSubs = new List<DataPlaneSubscriptionInfo>();

                        foreach (var dpSub in computeOptions.DataPlaneSubscriptions)
                        {
                            var dpSubInfo = new DataPlaneSubscriptionInfo()
                            {
                                SubscriptionId = dpSub,
                            };

                            var az = _azureClientFactory.GenerateLiftrAzure(dpSub);
                            var stors = await az.ListStorageAccountAsync(namingContext.ResourceGroupName(computeOptions.DataBaseName + "-dp"));
                            dpSubInfo.StorageAccountIds = stors.Select(st => st.Id);
                            dpSubs.Add(dpSubInfo);
                        }

                        rpAssets.DataPlaneSubscriptions = dpSubs;
                    }

                    _logger.Information("Puting the RPAssetOptions in the key vault ...");
                    await valet.SetSecretAsync($"{computeOptions.SecretPrefix}-{nameof(RPAssetOptions)}", rpAssets.ToJson(), namingContext.Tags);

                    _logger.Information($"Puting the {nameof(RunningEnvironmentOptions)} in the key vault ...");
                    var envOptions = new RunningEnvironmentOptions()
                    {
                        TenantId = regionalMsi.TenantId,
                        SPNObjectId = regionalMsi.GetObjectId(),
                    };

                    await valet.SetSecretAsync($"{computeOptions.SecretPrefix}-{nameof(RunningEnvironmentOptions)}-{nameof(envOptions.TenantId)}", envOptions.TenantId, namingContext.Tags);
                    await valet.SetSecretAsync($"{computeOptions.SecretPrefix}-{nameof(RunningEnvironmentOptions)}-{nameof(envOptions.SPNObjectId)}", envOptions.SPNObjectId, namingContext.Tags);

                    if (genevaCert != null)
                    {
                        _logger.Information("Creating AME Geneva certificate in Key Vault with name {@certName} ...", genevaCert.CertificateName);
                        var certIssuerName = "one-cert-issuer";
                        await valet.SetCertificateIssuerAsync(certIssuerName, "OneCert");
                        await valet.CreateCertificateAsync(genevaCert.CertificateName, certIssuerName, genevaCert.SubjectName, genevaCert.SubjectAlternativeNames, namingContext.Tags);
                        _logger.Information("Finished creating AME Geneva certificate in Key Vault with name {@certName}", genevaCert.CertificateName);
                    }

                    if (sslCert != null)
                    {
                        _logger.Information("Creating SSL certificate in Key Vault with name {@certName} ...", sslCert.CertificateName);
                        var certIssuerName = "one-cert-issuer";
                        await valet.SetCertificateIssuerAsync(certIssuerName, "OneCert");
                        await valet.CreateCertificateAsync(sslCert.CertificateName, certIssuerName, sslCert.SubjectName, sslCert.SubjectAlternativeNames, namingContext.Tags);
                        _logger.Information("Finished creating SSL certificate in Key Vault with name {@certName}", sslCert.CertificateName);
                    }

                    if (firstPartyCert != null)
                    {
                        _logger.Information("Creating First Party certificate in Key Vault with name {@certName} ...", firstPartyCert.CertificateName);
                        var certIssuerName = "one-cert-issuer";
                        await valet.SetCertificateIssuerAsync(certIssuerName, "OneCert");
                        await valet.CreateCertificateAsync(firstPartyCert.CertificateName, certIssuerName, firstPartyCert.SubjectName, firstPartyCert.SubjectAlternativeNames, namingContext.Tags);
                        _logger.Information("Finished creating First Party certificate in Key Vault with name {@certName}", firstPartyCert.CertificateName);
                    }
                }

                var targetResourceId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.ContainerService/managedClusters/{aksName}";
                aks = await liftrAzure.GetAksClusterAsync(targetResourceId);
                if (aks == null)
                {
                    _logger.Information("Creating AKS cluster ...");
                    aks = await liftrAzure.CreateAksClusterAsync(namingContext.Location, rgName, aksName, aksInfo.AKSRootUserName, aksInfo.AKSSSHPublicKey, aksInfo.AKSSPNClientId, aksSPNClientSecret.Value, aksInfo.AKSMachineType, aksInfo.AKSMachineCount, namingContext.Tags);
                    _logger.Information("Created AKS cluster with Id {ResourceId}", aks.Id);
                }
                else
                {
                    _logger.Information("Use existing AKS cluster with Id {ResourceId}.", aks.Id);
                }
            }

            return (kv, regionalMsi, aks);
        }
    }
}
