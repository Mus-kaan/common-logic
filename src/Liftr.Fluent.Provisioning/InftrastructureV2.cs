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
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Rest.Azure;
using Serilog;
using System;
using System.Data;
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

        public async Task<IVault> CreateOrUpdateGlobalRGAsync(string baseName, NamingContext namingContext, string kvAdminClientId)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

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

            _logger.Information("Creating Key Vault ...");
            var targetReousrceId = $"subscriptions/{client.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.KeyVault/vaults/{kvName}";
            kv = await client.GetKeyVaultByIdAsync(targetReousrceId);
            if (kv == null)
            {
                kv = await client.CreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags, kvAdminClientId);
                _logger.Information("Created KeyVault with Id {ResourceId}", kv.Id);
            }
            else
            {
                _logger.Information("There has an existing Key Vault with the same {ResourceId}. Stop creating a new one.", kv.Id);

                await kv.Update()
                    .DefineAccessPolicy()
                        .ForServicePrincipal(kvAdminClientId)
                        .AllowSecretAllPermissions()
                        .AllowCertificateAllPermissions()
                        .Attach()
                    .ApplyAsync();
            }

            return kv;
        }

        public async Task<(ICosmosDBAccount db, ITrafficManagerProfile tm)> CreateOrUpdateRegionalDataRGAsync(string baseName, NamingContext namingContext)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            ICosmosDBAccount db = null;
            ITrafficManagerProfile tm = null;

            var rgName = namingContext.ResourceGroupName(baseName);
            var trafficManagerName = namingContext.TrafficManagerName(baseName);
            var cosmosName = namingContext.CosmosDBName(baseName);

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

            _logger.Information("Creating Traffic Manager ...");
            var targetReousrceId = $"subscriptions/{client.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.Network/trafficmanagerprofiles/{trafficManagerName}";
            tm = await client.GetTrafficManagerAsync(targetReousrceId);
            if (tm == null)
            {
                tm = await client.CreateTrafficManagerAsync(rgName, trafficManagerName, namingContext.Tags);
                _logger.Information("Created Traffic Manager with Id {ResourceId}", tm.Id);
            }
            else
            {
                _logger.Information("There has an existing Traffic Manager with the same {ResourceId}. Stop creating a new one.", tm.Id);
            }

            _logger.Information("Creating CosmosDB ...");
            targetReousrceId = $"subscriptions/{client.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosName}";
            db = await client.GetCosmosDBAsync(targetReousrceId);
            if (db == null)
            {
                (var createdDb, string connectionString) = await client.CreateCosmosDBAsync(namingContext.Location, rgName, cosmosName, namingContext.Tags);
                db = createdDb;
                _logger.Information("Created CosmosDB with Id {ResourceId}", db.Id);
            }
            else
            {
                _logger.Information("There has an existing CosmosDB with the same {ResourceId}. Stop creating a new one.", db.Id);
            }

            return (db, tm);
        }

        public async Task<IVault> GetComputeKeyVaultAsync(string baseName, NamingContext namingContext)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            var rgName = namingContext.ResourceGroupName(baseName);
            var kvName = namingContext.KeyVaultName(baseName);

            var client = _azureClientFactory.GenerateLiftrAzure();
            var targetReousrceId = $"subscriptions/{client.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.KeyVault/vaults/{kvName}";
            var kv = await client.GetKeyVaultByIdAsync(targetReousrceId);
            return kv;
        }

        public async Task<(IVault kv, IIdentity msi, IKubernetesCluster aks)> CreateOrUpdateRegionalComputeRGAsync(string baseName, NamingContext namingContext, InfraV2RegionalComputeOptions computeOptions, AKSInfo aksInfo, KeyVaultClient kvClient, CertificateOptions genevaCert = null, CertificateOptions sslCert = null)
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
            IIdentity msi = null;
            IKubernetesCluster aks = null;

            var rgName = namingContext.ResourceGroupName(baseName);
            var msiName = namingContext.MSIName(baseName);
            var kvName = namingContext.KeyVaultName(baseName);
            var aksName = namingContext.AKSName(baseName);

            var client = _azureClientFactory.GenerateLiftrAzure();

            var db = await client.GetCosmosDBAsync(computeOptions.CosmosDBResourceId);
            if (db == null)
            {
                var ex = new InvalidOperationException("Cannot find cosmos DB with resource Id: " + computeOptions.CosmosDBResourceId);
                _logger.Error("Cannot find cosmos DB with resource Id: {ResourceId}", computeOptions.CosmosDBResourceId);
                throw ex;
            }

            var centralKv = await client.GetKeyVaultByIdAsync(computeOptions.CentralKeyVaultResourceId);
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
                    var rg = await client.CreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
                    _logger.Information("Created Resource Group with Id {ResourceId}", rg.Id);
                }
                catch (DuplicateNameException ex)
                {
                    _logger.Information("There exist a RG with the same name. Reuse it. {ExceptionDetail}", ex);
                }

                var targetReousrceId = $"subscriptions/{client.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{msiName}";
                msi = await client.GetMSIAsync(targetReousrceId);
                if (msi == null)
                {
                    _logger.Information("Creating MSI ...");
                    msi = await client.CreateMSIAsync(namingContext.Location, rgName, msiName, namingContext.Tags);
                    _logger.Information("Created MSI with Id {ResourceId}", msi.Id);

                    _logger.Information("Granting the identity binding access for the MSI {MSIId} to the AKS SPN with {AKSobjectId} ...", msi.Id, aksInfo.AKSSPNObjectId);
                    try
                    {
                        await client.Authenticated.RoleAssignments
                            .Define(SdkContext.RandomGuid())
                            .ForObjectId(aksInfo.AKSSPNObjectId)
                            .WithBuiltInRole(BuiltInRole.Contributor)
                            .WithResourceScope(msi)
                            .CreateAsync();
                        _logger.Information("Granted the identity binding access for the MSI {MSIId} to the AKS SPN with {AKSobjectId}.", msi.Id, aksInfo.AKSSPNObjectId);
                    }
                    catch (CloudException ex) when (ex.Message.Contains("The role assignment already exists"))
                    {
                        _logger.Information("There exists the same role assignment for the MSI {MSIId} to the AKS SPN with {AKSobjectId}.", msi.Id, aksInfo.AKSSPNObjectId);
                    }
                }
                else
                {
                    _logger.Information("Use existing MSI with id {ResourceId}.", msi.Id);
                }

                targetReousrceId = $"subscriptions/{client.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.KeyVault/vaults/{kvName}";
                kv = await client.GetKeyVaultByIdAsync(targetReousrceId);
                if (kv == null)
                {
                    _logger.Information("Creating Key Vault ...");
                    kv = await client.CreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags, computeOptions.ProvisioningSPNClientId);
                    _logger.Information("Created KeyVault with Id {ResourceId}", kv.Id);
                }
                else
                {
                    _logger.Information("Use existing Key Vault with Id {ResourceId}.", kv.Id);

                    await kv.Update()
                    .DefineAccessPolicy()
                        .ForServicePrincipal(computeOptions.ProvisioningSPNClientId)
                        .AllowSecretAllPermissions()
                        .AllowCertificateAllPermissions()
                        .Attach()
                    .ApplyAsync();
                }

                _logger.Information("Start adding access policy for msi to kv.");
                await kv.Update()
                    .DefineAccessPolicy()
                    .ForObjectId(msi.Inner.PrincipalId.Value.ToString())
                    .AllowSecretPermissions(SecretPermissions.List, SecretPermissions.Get)
                    .Attach()
                    .ApplyAsync();
                _logger.Information("Added access policy for msi to kv.");

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

                    _logger.Information("Puting the CosmosDB Connection String in the key vault ...");
                    var dbConnectionStrings = await db.ListConnectionStringsAsync();
                    await valet.SetSecretAsync(computeOptions.KVDBSecretName, dbConnectionStrings.ConnectionStrings[0].ConnectionString, namingContext.Tags);

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
                }

                targetReousrceId = $"subscriptions/{client.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.ContainerService/managedClusters/{aksName}";
                aks = await client.GetAksClusterAsync(targetReousrceId);
                if (aks == null)
                {
                    _logger.Information("Creating AKS cluster ...");
                    aks = await client.CreateAksClusterAsync(namingContext.Location, rgName, aksName, aksInfo.AKSRootUserName, aksInfo.AKSSSHPublicKey, aksInfo.AKSSPNClientId, aksSPNClientSecret.Value, aksInfo.AKSMachineType, aksInfo.AKSMachineCount, namingContext.Tags);
                    _logger.Information("Created AKS cluster with Id {ResourceId}", aks.Id);
                }
                else
                {
                    _logger.Information("Use existing AKS cluster with Id {ResourceId}.", aks.Id);
                }
            }

            return (kv, msi, aks);
        }
    }
}
