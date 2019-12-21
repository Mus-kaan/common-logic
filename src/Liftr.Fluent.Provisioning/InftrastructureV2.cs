//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
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

        public async Task<(IVault, IRegistry)> CreateOrUpdateGlobalRGAsync(string baseName, NamingContext namingContext)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            try
            {
                var rgName = namingContext.ResourceGroupName(baseName);
                var kvName = namingContext.KeyVaultName(baseName);
                var acrName = namingContext.ACRName(baseName);

                var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

                var rg = await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
                var kv = await liftrAzure.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags);
                await liftrAzure.GrantSelfKeyVaultAdminAccessAsync(kv);

                var acr = await liftrAzure.GetOrCreateACRAsync(namingContext.Location, rgName, acrName, namingContext.Tags);

                return (kv, acr);
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
            RegionalDataOptions dataOptions,
            KeyVaultClient kvClient)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            if (dataOptions == null)
            {
                throw new ArgumentNullException(nameof(dataOptions));
            }

            dataOptions.CheckValid();

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            ICosmosDBAccount db = null;
            ITrafficManagerProfile tm = null;
            IVault kv = null;

            var rgName = namingContext.ResourceGroupName(baseName);
            var storageName = namingContext.StorageAccountName(baseName);
            var trafficManagerName = namingContext.TrafficManagerName(baseName);
            var kvName = namingContext.KeyVaultName(baseName);
            var cosmosName = namingContext.CosmosDBName(baseName);
            var msiName = namingContext.MSIName(baseName);

            var rg = await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
            var msi = await liftrAzure.GetOrCreateMSIAsync(namingContext.Location, rgName, msiName, namingContext.Tags);
            var storageAccount = await liftrAzure.GetOrCreateStorageAccountAsync(namingContext.Location, rgName, storageName, namingContext.Tags);
            await liftrAzure.GrantQueueContributorAsync(storageAccount, msi);

            if (dataOptions.DataPlaneSubscriptions != null)
            {
                foreach (var subscrptionId in dataOptions.DataPlaneSubscriptions)
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
                    }
                }
            }

            tm = await liftrAzure.GetOrCreateTrafficManagerAsync(rgName, trafficManagerName, namingContext.Tags);
            kv = await liftrAzure.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags);
            await liftrAzure.GrantSelfKeyVaultAdminAccessAsync(kv);

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
            var targetResourceId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosName}";
            db = await liftrAzure.GetCosmosDBAsync(targetResourceId);
            if (db == null)
            {
                (var createdDb, _) = await liftrAzure.CreateCosmosDBAsync(namingContext.Location, rgName, cosmosName, namingContext.Tags);
                db = createdDb;
                _logger.Information("Created CosmosDB with Id {ResourceId}", db.Id);
            }

            if (dataOptions.DataPlaneStorageCountPerSubscription > 0 && dataOptions.DataPlaneSubscriptions != null)
            {
                foreach (var dpSubscription in dataOptions.DataPlaneSubscriptions)
                {
                    var dataPlaneLiftrAzure = _azureClientFactory.GenerateLiftrAzure(dpSubscription);
                    IResourceGroup dataPlaneStorageRG = await dataPlaneLiftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, namingContext.ResourceGroupName(baseName + "-dp"), namingContext.Tags);

                    var existingStorageAccountCount = (await dataPlaneLiftrAzure.ListStorageAccountAsync(dataPlaneStorageRG.Name)).Count();

                    for (int i = existingStorageAccountCount; i < dataOptions.DataPlaneStorageCountPerSubscription; i++)
                    {
                        var storageAccountName = SdkContext.RandomResourceName(baseName, 24);
                        await dataPlaneLiftrAzure.GetOrCreateStorageAccountAsync(namingContext.Location, dataPlaneStorageRG.Name, storageAccountName, namingContext.Tags);
                    }
                }
            }

            using (var regionalKVValet = new KeyVaultConcierge(kv.VaultUri, kvClient, _logger))
            {
                var rpAssets = new RPAssetOptions()
                {
                    StorageAccountName = storageAccount.Name,
                    ActiveKeyName = dataOptions.ActiveDBKeyName,
                };

                var dbConnectionStrings = await db.ListConnectionStringsAsync();
                rpAssets.CosmosDBConnectionStrings = dbConnectionStrings.ConnectionStrings.Select(c => new CosmosDBConnectionString()
                {
                    ConnectionString = c.ConnectionString,
                    Description = c.Description,
                });

                if (dataOptions.DataPlaneSubscriptions != null)
                {
                    var dataPlaneSubscriptionInfos = new List<DataPlaneSubscriptionInfo>();

                    foreach (var dataPlaneSubscriptionId in dataOptions.DataPlaneSubscriptions)
                    {
                        var dpSubInfo = new DataPlaneSubscriptionInfo()
                        {
                            SubscriptionId = dataPlaneSubscriptionId,
                        };

                        var dataPlaneSubscription = _azureClientFactory.GenerateLiftrAzure(dataPlaneSubscriptionId);
                        var stors = await dataPlaneSubscription.ListStorageAccountAsync(namingContext.ResourceGroupName(baseName + "-dp"));
                        dpSubInfo.StorageAccountIds = stors.Select(st => st.Id);
                        dataPlaneSubscriptionInfos.Add(dpSubInfo);
                    }

                    rpAssets.DataPlaneSubscriptions = dataPlaneSubscriptionInfos;
                }

                _logger.Information("Puting the RPAssetOptions in the key vault ...");
                await regionalKVValet.SetSecretAsync($"{dataOptions.SecretPrefix}-{nameof(RPAssetOptions)}", rpAssets.ToJson(), namingContext.Tags);

                var envOptions = new RunningEnvironmentOptions()
                {
                    TenantId = msi.TenantId,
                    SPNObjectId = msi.GetObjectId(),
                };

                _logger.Information($"Puting the {nameof(RunningEnvironmentOptions)} in the key vault ...");
                await regionalKVValet.SetSecretAsync($"{dataOptions.SecretPrefix}-{nameof(RunningEnvironmentOptions)}--{nameof(envOptions.TenantId)}", envOptions.TenantId, namingContext.Tags);
                await regionalKVValet.SetSecretAsync($"{dataOptions.SecretPrefix}-{nameof(RunningEnvironmentOptions)}--{nameof(envOptions.SPNObjectId)}", envOptions.SPNObjectId, namingContext.Tags);

                var certIssuerName = "one-cert-issuer";
                if (dataOptions.GenevaCert != null)
                {
                    _logger.Information("Creating AME Geneva certificate in Key Vault with name {@certName} ...", dataOptions.GenevaCert.CertificateName);
                    await regionalKVValet.SetCertificateIssuerAsync(certIssuerName, "OneCert");
                    await regionalKVValet.CreateCertificateAsync(dataOptions.GenevaCert.CertificateName, certIssuerName, dataOptions.GenevaCert.SubjectName, dataOptions.GenevaCert.SubjectAlternativeNames, namingContext.Tags);
                    _logger.Information("Finished creating AME Geneva certificate in Key Vault with name {@certName}", dataOptions.GenevaCert.CertificateName);
                }

                if (dataOptions.SSLCert != null)
                {
                    _logger.Information("Creating SSL certificate in Key Vault with name {@certName} ...", dataOptions.SSLCert.CertificateName);
                    await regionalKVValet.SetCertificateIssuerAsync(certIssuerName, "OneCert");
                    await regionalKVValet.CreateCertificateAsync(dataOptions.SSLCert.CertificateName, certIssuerName, dataOptions.SSLCert.SubjectName, dataOptions.SSLCert.SubjectAlternativeNames, namingContext.Tags);
                    _logger.Information("Finished creating SSL certificate in Key Vault with name {@certName}", dataOptions.SSLCert.CertificateName);
                }

                if (dataOptions.FirstPartyCert != null)
                {
                    _logger.Information("Creating First Party certificate in Key Vault with name {@certName} ...", dataOptions.FirstPartyCert.CertificateName);
                    await regionalKVValet.SetCertificateIssuerAsync(certIssuerName, "OneCert");
                    await regionalKVValet.CreateCertificateAsync(dataOptions.FirstPartyCert.CertificateName, certIssuerName, dataOptions.FirstPartyCert.SubjectName, dataOptions.FirstPartyCert.SubjectAlternativeNames, namingContext.Tags);
                    _logger.Information("Finished creating First Party certificate in Key Vault with name {@certName}", dataOptions.FirstPartyCert.CertificateName);
                }
            }

            return (rg, msi, db, tm, kv);
        }

        public async Task<(IVault kv, IIdentity msi, IKubernetesCluster aks)> CreateOrUpdateRegionalComputeRGAsync(
            NamingContext namingContext,
            RegionalComputeOptions computeOptions,
            AKSInfo aksInfo,
            KeyVaultClient kvClient)
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

            _logger.Information("AKS machine type: {AKSMachineType}", aksInfo.AKSMachineType.Value);
            _logger.Information("AKS machine count: {AKSMachineCount}", aksInfo.AKSMachineCount);

            var rgName = namingContext.ResourceGroupName(computeOptions.ComputeBaseName);
            var aksName = namingContext.AKSName(computeOptions.ComputeBaseName);

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            var msiName = namingContext.MSIName(computeOptions.DataBaseName);
            var msi = await liftrAzure.GetMSIAsync(namingContext.ResourceGroupName(computeOptions.DataBaseName), msiName);
            if (msi == null)
            {
                var ex = new InvalidOperationException("Cannot find regional MSI with resource name: " + msiName);
                _logger.Error("Cannot find regional MSI with resource name: {ResourceName}", msiName);
                throw ex;
            }

            var kvName = namingContext.KeyVaultName(computeOptions.DataBaseName);
            var kv = await liftrAzure.GetKeyVaultAsync(namingContext.ResourceGroupName(computeOptions.DataBaseName), kvName);
            if (kv == null)
            {
                var ex = new InvalidOperationException("Cannot find regional key vault with resource name: " + kvName);
                _logger.Error("Cannot find regional key vault with resource name: {ResourceName}", kvName);
                throw ex;
            }

            try
            {
                _logger.Information("Granting the identity binding access for the MSI {MSIResourceId} to the AKS SPN with {AKSobjectId} ...", msi.Id, aksInfo.AKSSPNObjectId);
                await liftrAzure.Authenticated.RoleAssignments
                    .Define(SdkContext.RandomGuid())
                    .ForObjectId(aksInfo.AKSSPNObjectId)
                    .WithBuiltInRole(BuiltInRole.Contributor)
                    .WithResourceScope(msi)
                    .CreateAsync();
                _logger.Information("Granted the identity binding access for the MSI {MSIResourceId} to the AKS SPN with {AKSobjectId}.", msi.Id, aksInfo.AKSSPNObjectId);
            }
            catch (CloudException ex) when (ex.Message.Contains("The role assignment already exists"))
            {
            }

            try
            {
                // ACR Pull
                var roleDefinitionId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/7f951dda-4ed3-4680-a7ca-43fe172d538d";
                _logger.Information("Granting ACR pull role to the AKS SPN {AKSSPNObjectId} for the subscription {subscrptionId} ...", aksInfo.AKSSPNObjectId, liftrAzure.FluentClient.SubscriptionId);
                await liftrAzure.Authenticated.RoleAssignments
                    .Define(SdkContext.RandomGuid())
                    .ForObjectId(aksInfo.AKSSPNObjectId)
                    .WithRoleDefinition(roleDefinitionId)
                    .WithSubscriptionScope(liftrAzure.FluentClient.SubscriptionId)
                    .CreateAsync();
            }
            catch (CloudException ex) when (ex.Message.Contains("The role assignment already exists"))
            {
            }

            var globalKv = await liftrAzure.GetKeyVaultByIdAsync(computeOptions.GlobalKeyVaultResourceId);
            if (globalKv == null)
            {
                var ex = new InvalidOperationException("Cannot find central key vault with resource Id: " + computeOptions.GlobalKeyVaultResourceId);
                _logger.Error("Cannot find central key vault with resource Id: {ResourceId}", computeOptions.GlobalKeyVaultResourceId);
                throw ex;
            }

            string aksSPNClientSecret = null;
            using (var globalKVValet = new KeyVaultConcierge(globalKv.VaultUri, kvClient, _logger))
            {
                aksSPNClientSecret = (await globalKVValet.GetSecretAsync(aksInfo.AKSSPNClientSecretName))?.Value;
                if (aksSPNClientSecret == null)
                {
                    var errorMsg = $"Cannot find the AKS SPN client secret in key vault with secret name: {aksInfo.AKSSPNClientSecretName}";
                    _logger.Error(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }
            }

            await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);

            var targetResourceId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.ContainerService/managedClusters/{aksName}";
            var aks = await liftrAzure.GetAksClusterAsync(targetResourceId);
            if (aks == null)
            {
                _logger.Information("Creating AKS cluster ...");
                aks = await liftrAzure.CreateAksClusterAsync(namingContext.Location, rgName, aksName, aksInfo.AKSRootUserName, aksInfo.AKSSSHPublicKey, aksInfo.AKSSPNClientId, aksSPNClientSecret, aksInfo.AKSMachineType, aksInfo.AKSMachineCount, namingContext.Tags);
                _logger.Information("Created AKS cluster with Id {ResourceId}", aks.Id);
            }
            else
            {
                _logger.Information("Use existing AKS cluster with Id {ResourceId}.", aks.Id);
            }

            return (kv, msi, aks);
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
    }
}
