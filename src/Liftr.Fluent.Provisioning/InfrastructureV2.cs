﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Rest.Azure;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class InfrastructureV2
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

        public async Task<ProvisionedGlobalResources> CreateOrUpdateGlobalRGAsync(
            string baseName,
            NamingContext namingContext,
            string dnsName,
            string logAnalyticsWorkspaceId = null)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            ProvisionedGlobalResources result = new ProvisionedGlobalResources();

            try
            {
                var rgName = namingContext.ResourceGroupName(baseName);
                var kvName = namingContext.KeyVaultName(baseName);
                var acrName = namingContext.ACRName(baseName);

                var liftrAzure = _azureClientFactory.GenerateLiftrAzure();
                result.ResourceGroup = await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
                if (string.IsNullOrEmpty(logAnalyticsWorkspaceId))
                {
                    var logAnalyticsName = namingContext.LogAnalyticsName(baseName);
                    var logAnalytics = await liftrAzure.GetOrCreateLogAnalyticsWorkspaceAsync(namingContext.Location, rgName, logAnalyticsName, namingContext.Tags);
                    logAnalyticsWorkspaceId = logAnalytics.Id;
                }

                result.DnsZone = await liftrAzure.GetDNSZoneAsync(rgName, dnsName);
                if (result.DnsZone == null)
                {
                    result.DnsZone = await liftrAzure.CreateDNSZoneAsync(rgName, dnsName, namingContext.Tags);
                }

                result.KeyVault = await liftrAzure.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags);

                _logger.Information("Export Key Vault '{kvId}' diagnostics to Log Analytics '{logId}'.", result.KeyVault.Id, logAnalyticsWorkspaceId);
                await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(result.KeyVault, logAnalyticsWorkspaceId);
                await liftrAzure.GrantSelfKeyVaultAdminAccessAsync(result.KeyVault);

                result.ContainerRegistry = await liftrAzure.GetOrCreateACRAsync(namingContext.Location, rgName, acrName, namingContext.Tags);
                _logger.Information("Export ACR '{acrId}' diagnostics to Log Analytics '{logId}'.", result.ContainerRegistry.Id, logAnalyticsWorkspaceId);
                await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(result.ContainerRegistry, logAnalyticsWorkspaceId);

                var diagnosticsStorName = namingContext.StorageAccountName(baseName);
                var stor = await liftrAzure.GetOrCreateStorageAccountAsync(namingContext.Location, rgName, diagnosticsStorName, namingContext.Tags);

                using (var kvValet = new KeyVaultConcierge(result.KeyVault.VaultUri, _kvClient, _logger))
                {
                    if (!await kvValet.ContainsSecretAsync(SSHUserNameSecretName))
                    {
                        _logger.Information("Storing SSH user name in global key vault.");
                        await kvValet.SetSecretAsync(SSHUserNameSecretName, "liftraksvmuser", namingContext.Tags);
                    }

                    if (File.Exists("liftr_ssh_key") && !await kvValet.ContainsSecretAsync(SSHPrivateKeySecretName))
                    {
                        _logger.Information("Storing SSH private key in global key vault.");
                        var sshPrivateKey = File.ReadAllText("liftr_ssh_key");
                        await kvValet.SetSecretAsync(SSHPrivateKeySecretName, sshPrivateKey, namingContext.Tags);
                    }

                    if (File.Exists("liftr_ssh_key.pub") && !await kvValet.ContainsSecretAsync(SSHPublicKeySecretName))
                    {
                        _logger.Information("Storing SSH public key in global key vault.");
                        var sshPublicKey = File.ReadAllText("liftr_ssh_key.pub");
                        await kvValet.SetSecretAsync(SSHPublicKeySecretName, sshPublicKey, namingContext.Tags);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(CreateOrUpdateGlobalRGAsync)} failed.");
                throw;
            }
        }

        public async Task<ProvisionedRegionalDataResources> CreateOrUpdateRegionalDataRGAsync(
            string baseName,
            NamingContext namingContext,
            RegionalDataOptions dataOptions)
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

            ProvisionedRegionalDataResources provisionedResources = new ProvisionedRegionalDataResources();
            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            var rgName = namingContext.ResourceGroupName(baseName);
            var storageName = namingContext.StorageAccountName(baseName);
            var trafficManagerName = namingContext.TrafficManagerName(baseName);
            var kvName = namingContext.KeyVaultName(baseName);
            var cosmosName = namingContext.CosmosDBName(baseName);
            var msiName = namingContext.MSIName(baseName);
            var currentPublicIP = await MetadataHelper.GetPublicIPAddressAsync();
            _logger.Information("Current public IP address: {currentPublicIP}", currentPublicIP);

            var dnsZoneId = new ResourceId(dataOptions.DNSZoneId);
            provisionedResources.DnsZone = await liftrAzure.GetDNSZoneAsync(dnsZoneId.ResourceGroup, dnsZoneId.ResourceName);
            if (provisionedResources.DnsZone == null)
            {
                provisionedResources.DnsZone = await liftrAzure.CreateDNSZoneAsync(dnsZoneId.ResourceGroup, dnsZoneId.ResourceName, namingContext.Tags);
            }

            var rg = await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
            provisionedResources.ResourceGroup = rg;
            provisionedResources.ManagedIdentity = await liftrAzure.GetOrCreateMSIAsync(namingContext.Location, rgName, msiName, namingContext.Tags);

            ISubnet subnet = null;
            if (dataOptions.EnableVNet)
            {
                var vnetName = namingContext.NetworkName(baseName);
                var nsgName = $"{vnetName}-default-nsg";
                var nsg = await liftrAzure.GetOrCreateDefaultNSGAsync(namingContext.Location, rgName, nsgName, namingContext.Tags);
                provisionedResources.VNet = await liftrAzure.GetOrCreateVNetAsync(namingContext.Location, rgName, vnetName, namingContext.Tags, nsg.Id);
                await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(provisionedResources.VNet, dataOptions.LogAnalyticsWorkspaceId);
                subnet = provisionedResources.VNet.Subnets[liftrAzure.DefaultSubnetName];
                provisionedResources.KeyVault = await liftrAzure.GetKeyVaultAsync(rgName, kvName);
                if (provisionedResources.KeyVault == null)
                {
                    provisionedResources.KeyVault = await liftrAzure.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, currentPublicIP, namingContext.Tags);
                }
                else
                {
                    _logger.Information("Make sure the Key Vault '{kvId}' can be accessed from current IP '{currentPublicIP}'.", provisionedResources.KeyVault.Id, currentPublicIP);
                    await liftrAzure.WithKeyVaultAccessFromNetworkAsync(provisionedResources.KeyVault, currentPublicIP, subnet?.Inner?.Id);
                }
            }
            else
            {
                provisionedResources.KeyVault = await liftrAzure.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags);
            }

            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();
            await liftrAzure.GrantSelfKeyVaultAdminAccessAsync(provisionedResources.KeyVault);

            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();
            await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(provisionedResources.KeyVault, dataOptions.LogAnalyticsWorkspaceId);
            provisionedResources.KeyVault = provisionedResources.KeyVault;

            provisionedResources.StorageAccount = await liftrAzure.GetOrCreateStorageAccountAsync(namingContext.Location, rgName, storageName, namingContext.Tags, subnet?.Inner?.Id);
            await liftrAzure.GrantQueueContributorAsync(provisionedResources.StorageAccount, provisionedResources.ManagedIdentity);

            if (dataOptions.DataPlaneSubscriptions != null)
            {
                foreach (var subscrptionId in dataOptions.DataPlaneSubscriptions)
                {
                    try
                    {
                        _logger.Information("Granting the MSI {MSIReourceId} contributor role to the subscription with {subscrptionId} ...", provisionedResources.ManagedIdentity.Id, subscrptionId);
                        await liftrAzure.Authenticated.RoleAssignments
                            .Define(SdkContext.RandomGuid())
                            .ForObjectId(provisionedResources.ManagedIdentity.GetObjectId())
                            .WithBuiltInRole(BuiltInRole.Contributor)
                            .WithSubscriptionScope(subscrptionId)
                            .CreateAsync();
                    }
                    catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                    {
                    }
                }
            }

            provisionedResources.TrafficManager = await liftrAzure.GetOrCreateTrafficManagerAsync(rgName, trafficManagerName, namingContext.Tags);
            await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(provisionedResources.TrafficManager, dataOptions.LogAnalyticsWorkspaceId);

            _logger.Information("Set DNS zone '{dnsZone}' CNAME '{cname}' to Traffic Manager '{tmFqdn}'.", provisionedResources.DnsZone.Id, namingContext.Location.ShortName(), provisionedResources.TrafficManager.Fqdn);
            await provisionedResources.DnsZone.Update()
                .DefineCNameRecordSet(namingContext.Location.ShortName())
                .WithAlias(provisionedResources.TrafficManager.Fqdn).WithTimeToLive(600)
                .Attach()
                .DefineCNameRecordSet($"*.{namingContext.Location.ShortName()}")
                .WithAlias(provisionedResources.TrafficManager.Fqdn).WithTimeToLive(600)
                .Attach()
                .ApplyAsync();

            _logger.Information("Start adding access policy for msi to regional kv.");
            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();
            await provisionedResources.KeyVault.Update()
                .DefineAccessPolicy()
                .ForObjectId(provisionedResources.ManagedIdentity.GetObjectId())
                .AllowSecretPermissions(SecretPermissions.List, SecretPermissions.Get)
                .AllowCertificatePermissions(CertificatePermissions.Get, CertificatePermissions.Getissuers, CertificatePermissions.List)
                .Attach()
                .ApplyAsync();
            _logger.Information("Added access policy for msi to regional kv.");

            var targetResourceId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosName}";
            provisionedResources.CosmosDBAccount = await liftrAzure.GetCosmosDBAsync(targetResourceId);
            if (provisionedResources.CosmosDBAccount == null)
            {
                (var createdDb, _) = await liftrAzure.CreateCosmosDBAsync(namingContext.Location, rgName, cosmosName, namingContext.Tags, subnet);
                provisionedResources.CosmosDBAccount = createdDb;
                await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(provisionedResources.CosmosDBAccount, dataOptions.LogAnalyticsWorkspaceId);
                _logger.Information("Created CosmosDB with Id {ResourceId}", provisionedResources.CosmosDBAccount.Id);
            }

            if (dataOptions.DataPlaneStorageCountPerSubscription > 0 && dataOptions.DataPlaneSubscriptions != null)
            {
                foreach (var dpSubscription in dataOptions.DataPlaneSubscriptions)
                {
                    var dataPlaneLiftrAzure = _azureClientFactory.GenerateLiftrAzure(dpSubscription);
                    var dataPlaneStorageRG = await dataPlaneLiftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, namingContext.ResourceGroupName(baseName + "-dp"), namingContext.Tags);
                    var existingStorageAccountCount = (await dataPlaneLiftrAzure.ListStorageAccountAsync(dataPlaneStorageRG.Name)).Count();

                    for (int i = existingStorageAccountCount; i < dataOptions.DataPlaneStorageCountPerSubscription; i++)
                    {
                        var storageAccountName = SdkContext.RandomResourceName(baseName, 24);
                        await dataPlaneLiftrAzure.GetOrCreateStorageAccountAsync(namingContext.Location, dataPlaneStorageRG.Name, storageAccountName, namingContext.Tags);
                    }
                }
            }

            using (var regionalKVValet = new KeyVaultConcierge(provisionedResources.KeyVault.VaultUri, _kvClient, _logger))
            {
                var rpAssets = new RPAssetOptions()
                {
                    StorageAccountName = provisionedResources.StorageAccount.Name,
                    ActiveKeyName = dataOptions.ActiveDBKeyName,
                };

                var dbConnectionStrings = await provisionedResources.CosmosDBAccount.ListConnectionStringsAsync();
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

                        if (dataOptions.DataPlaneStorageCountPerSubscription > 0)
                        {
                            var dataPlaneLiftrAzure = _azureClientFactory.GenerateLiftrAzure(dataPlaneSubscriptionId);
                            var stors = await dataPlaneLiftrAzure.ListStorageAccountAsync(namingContext.ResourceGroupName(baseName + "-dp"));
                            dpSubInfo.StorageAccountIds = stors.Select(st => st.Id);
                        }

                        dataPlaneSubscriptionInfos.Add(dpSubInfo);
                    }

                    rpAssets.DataPlaneSubscriptions = dataPlaneSubscriptionInfos;
                }

                _logger.Information("Puting the RPAssetOptions in the key vault ...");
                await regionalKVValet.SetSecretAsync($"{dataOptions.SecretPrefix}-{nameof(RPAssetOptions)}", rpAssets.ToJson(), namingContext.Tags);

                var envOptions = new RunningEnvironmentOptions()
                {
                    TenantId = provisionedResources.ManagedIdentity.TenantId,
                    SPNObjectId = provisionedResources.ManagedIdentity.GetObjectId(),
                };

                _logger.Information($"Puting the {nameof(RunningEnvironmentOptions)} in the key vault ...");
                await regionalKVValet.SetSecretAsync($"{dataOptions.SecretPrefix}-{nameof(RunningEnvironmentOptions)}--{nameof(envOptions.TenantId)}", envOptions.TenantId, namingContext.Tags);
                await regionalKVValet.SetSecretAsync($"{dataOptions.SecretPrefix}-{nameof(RunningEnvironmentOptions)}--{nameof(envOptions.SPNObjectId)}", envOptions.SPNObjectId, namingContext.Tags);

                // Move the secrets from global key vault to regional key vault.
                if (!string.IsNullOrEmpty(dataOptions.GlobalKeyVaultResourceId))
                {
                    var globalKv = await liftrAzure.GetKeyVaultByIdAsync(dataOptions.GlobalKeyVaultResourceId);
                    if (globalKv == null)
                    {
                        throw new InvalidOperationException($"Cannot find the global key vault with resource Id '{dataOptions.GlobalKeyVaultResourceId}'");
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
                }

                await CreateCertificatesAsync(regionalKVValet, dataOptions.OneCertCertificates, namingContext, dataOptions.DomainName);
            }

            return provisionedResources;
        }

        public async Task<(IVault kv, IIdentity msi, IKubernetesCluster aks)> CreateOrUpdateRegionalComputeRGAsync(
            NamingContext namingContext,
            RegionalComputeOptions computeOptions,
            AKSInfo aksInfo,
            KeyVaultClient _kvClient,
            bool enableVNet)
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
            var currentPublicIP = await MetadataHelper.GetPublicIPAddressAsync();
            _logger.Information("Current public IP address: {currentPublicIP}", currentPublicIP);

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
            var dataRGName = namingContext.ResourceGroupName(computeOptions.DataBaseName);
            var vnet = await liftrAzure.GetVNetAsync(dataRGName, namingContext.NetworkName(computeOptions.DataBaseName));
            INetworkSecurityGroup nsg = null;
            if (vnet != null)
            {
                var nsgName = $"{vnet.Name}-default-nsg";
                nsg = await liftrAzure.GetNSGAsync(dataRGName, nsgName);
                if (nsg == null)
                {
                    var ex = new InvalidOperationException($"Cannot the NSG with resource name '{nsgName}' in Resource Group '{dataRGName}'.");
                    _logger.Error("Cannot the NSG with resource name: {nsgName} in {dataRGName}", nsgName, dataRGName);
                    throw ex;
                }
            }

            var msiName = namingContext.MSIName(computeOptions.DataBaseName);
            var msi = await liftrAzure.GetMSIAsync(namingContext.ResourceGroupName(computeOptions.DataBaseName), msiName);
            if (msi == null)
            {
                var ex = new InvalidOperationException("Cannot find regional MSI with resource name: " + msiName);
                _logger.Error("Cannot find regional MSI with resource name: {ResourceName}", msiName);
                throw ex;
            }

            var kvName = namingContext.KeyVaultName(computeOptions.DataBaseName);
            var regionalKeyVault = await liftrAzure.GetKeyVaultAsync(namingContext.ResourceGroupName(computeOptions.DataBaseName), kvName);
            if (regionalKeyVault == null)
            {
                var ex = new InvalidOperationException("Cannot find regional key vault with resource name: " + kvName);
                _logger.Error("Cannot find regional key vault with resource name: {ResourceName}", kvName);
                throw ex;
            }

            try
            {
                _logger.Information("Granting the identity binding access for the MSI {MSIResourceId} to the AKS SPN with object Id '{AKSobjectId}' ...", msi.Id, aksInfo.AKSSPNObjectId);
                await liftrAzure.Authenticated.RoleAssignments
                    .Define(SdkContext.RandomGuid())
                    .ForObjectId(aksInfo.AKSSPNObjectId)
                    .WithBuiltInRole(BuiltInRole.Contributor)
                    .WithResourceScope(msi)
                    .CreateAsync();
                _logger.Information("Granted the identity binding access for the MSI {MSIResourceId} to the AKS SPN with object Id '{AKSobjectId}'.", msi.Id, aksInfo.AKSSPNObjectId);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
            catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
            {
                _logger.Error("The AKS SPN object Id '{AKSobjectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", aksInfo.AKSSPNObjectId);
                throw;
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
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
            catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
            {
                _logger.Error("The AKS SPN object Id '{AKSobjectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", aksInfo.AKSSPNObjectId);
                throw;
            }

            var globalKeyVault = await liftrAzure.GetKeyVaultByIdAsync(computeOptions.GlobalKeyVaultResourceId);
            if (globalKeyVault == null)
            {
                var ex = new InvalidOperationException("Cannot find central key vault with resource Id: " + computeOptions.GlobalKeyVaultResourceId);
                _logger.Error("Cannot find central key vault with resource Id: {ResourceId}", computeOptions.GlobalKeyVaultResourceId);
                throw ex;
            }

            string aksSPNClientSecret = null;
            string sshUserName = null;
            string sshPublicKey = null;
            using (var globalKVValet = new KeyVaultConcierge(globalKeyVault.VaultUri, _kvClient, _logger))
            {
                aksSPNClientSecret = (await globalKVValet.GetSecretAsync(aksInfo.AKSSPNClientSecretName))?.Value;
                if (aksSPNClientSecret == null)
                {
                    var errorMsg = $"Cannot find the AKS SPN client secret in key vault with secret name: {aksInfo.AKSSPNClientSecretName}";
                    _logger.Error(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                sshUserName = (await globalKVValet.GetSecretAsync(SSHUserNameSecretName))?.Value ?? throw new InvalidOperationException("Cannot find ssh user name in key vault");
                sshPublicKey = (await globalKVValet.GetSecretAsync(SSHPublicKeySecretName))?.Value ?? throw new InvalidOperationException("Cannot find ssh public key in key vault");
            }

            var aksId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.ContainerService/managedClusters/{aksName}";
            var aks = await liftrAzure.GetAksClusterAsync(aksId);
            if (aks == null)
            {
                var subnet = enableVNet ? await liftrAzure.CreateNewSubnetAsync(vnet, namingContext.SubnetName(computeOptions.ComputeBaseName), nsg?.Id) : null;

                var storName = namingContext.StorageAccountName(computeOptions.DataBaseName);
                var stor = await liftrAzure.GetStorageAccountAsync(dataRGName, storName);
                if (stor == null)
                {
                    var ex = new InvalidOperationException($"Cannot find the storage account with name '{storName}' in the data resource group '{dataRGName}'.");
                    _logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                var dbName = namingContext.CosmosDBName(computeOptions.DataBaseName);
                var db = await liftrAzure.GetCosmosDBAsync(dataRGName, dbName);
                if (db == null)
                {
                    var ex = new InvalidOperationException($"Cannot find the cosmos DB with name '{dbName}' in the data resource group '{dataRGName}'.");
                    _logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                if (enableVNet)
                {
                    _logger.Information("Restrict the Key Vault '{kvId}' to IP '{currentPublicIP}' and subnet '{subnetId}'.", regionalKeyVault.Id, currentPublicIP, subnet?.Inner?.Id);
                    await liftrAzure.WithKeyVaultAccessFromNetworkAsync(regionalKeyVault, currentPublicIP, subnet?.Inner?.Id);

                    _logger.Information("Restrict access to storage account with Id {storId} to subnet {subnetId}.", stor.Id, subnet.Inner.Id);
                    await stor.Update().WithAccessFromNetworkSubnet(subnet.Inner.Id).ApplyAsync();

                    _logger.Information("Restrict access to cosmos DB with Id {cosmosDBId} to subnet {subnetId}.", db.Id, subnet.Inner.Id);
                    await db.Update().WithVirtualNetwork(vnet.Id, subnet.Name).ApplyAsync();

                    try
                    {
                        _logger.Information("Make sure the AKS SPN '{AKSSPNObjectId}' has write access to the subnet '{subnetId}'.", aksInfo.AKSSPNObjectId, subnet.Inner.Id);
                        await liftrAzure.Authenticated.RoleAssignments
                            .Define(SdkContext.RandomGuid())
                            .ForObjectId(aksInfo.AKSSPNObjectId)
                            .WithBuiltInRole(BuiltInRole.NetworkContributor)
                            .WithScope(subnet.Inner.Id)
                            .CreateAsync();
                        _logger.Information("Network contributor role is assigned to the AKS SPN '{AKSSPNObjectId}' for the subnet '{subnetId}'.", aksInfo.AKSSPNObjectId, subnet.Inner.Id);
                    }
                    catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                    {
                    }
                    catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
                    {
                        _logger.Error("The AKS SPN object Id '{AKSobjectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", aksInfo.AKSSPNObjectId);
                        throw;
                    }
                }

                var agentPoolName = (namingContext.ShortPartnerName + namingContext.ShortEnvironmentName + namingContext.Location.ShortName()).ToLowerInvariant();
                if (agentPoolName.Length > 11)
                {
                    agentPoolName = agentPoolName.Substring(0, 11);
                }

                _logger.Information("Computed AKS agent pool profile name: {agentPoolName}", agentPoolName);

                _logger.Information("Creating AKS cluster ...");
                aks = await liftrAzure.CreateAksClusterAsync(
                    namingContext.Location,
                    rgName,
                    aksName,
                    sshUserName,
                    sshPublicKey,
                    aksInfo.AKSSPNClientId,
                    aksSPNClientSecret,
                    aksInfo.AKSMachineType,
                    aksInfo.AKSMachineCount,
                    namingContext.Tags,
                    subnet,
                    agentPoolProfileName: agentPoolName);

                _logger.Information("Created AKS cluster with Id {ResourceId}", aks.Id);

                if (!string.IsNullOrEmpty(computeOptions.LogAnalyticsWorkspaceResourceId))
                {
                    var aksAddOns = new Dictionary<string, ManagedClusterAddonProfile>()
                    {
                        ["omsagent"] = new ManagedClusterAddonProfile(true, new Dictionary<string, string>()
                        {
                            ["logAnalyticsWorkspaceResourceID"] = computeOptions.LogAnalyticsWorkspaceResourceId,
                        }),
                    };
                    _logger.Information("Enable AKS Azure Monitor and send the diagnostics data to Log Analytics with Id '{logAnalyticsWorkspaceResourceId}'", computeOptions.LogAnalyticsWorkspaceResourceId);
                    await aks.Update().WithAddOnProfiles(aksAddOns).ApplyAsync();
                }
            }
            else
            {
                _logger.Information("Use existing AKS cluster (ProvisioningState: {ProvisioningState}) with Id '{ResourceId}'.", aks.ProvisioningState, aks.Id);

                if (enableVNet)
                {
                    _logger.Information("Restrict the Key Vault '{kvId}' to IP '{currentPublicIP}'.", regionalKeyVault.Id, currentPublicIP);
                    await liftrAzure.WithKeyVaultAccessFromNetworkAsync(regionalKeyVault, currentPublicIP, null);
                }
            }

            return (regionalKeyVault, msi, aks);
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
            _logger.Information("Creating SSL certificate in Key Vault with name {certName} ...", CertificateName.DefaultSSL);
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
            await kvValet.CreateCertificateAsync(sslCert.CertificateName, OneCertIssuerName, sslCert.SubjectName, sslCert.SubjectAlternativeNames, namingContext.Tags);

            foreach (var cert in certificates)
            {
                var certName = cert.Key;
                var certSubject = cert.Value;
                if (cert.Key.OrdinalEquals(CertificateName.DefaultSSL))
                {
                    continue;
                }

                _logger.Information("Creating OneCert certificate in Key Vault with name '{certName}' and subject '{certSubject}'...", certName, certSubject);
                var certOptions = new CertificateOptions()
                {
                    CertificateName = certName,
                    SubjectName = certSubject,
                    SubjectAlternativeNames = new List<string>() { certSubject },
                };

                await kvValet.SetCertificateIssuerAsync(OneCertIssuerName, OneCertProvider);
                await kvValet.CreateCertificateAsync(certOptions.CertificateName, OneCertIssuerName, certOptions.SubjectName, certOptions.SubjectAlternativeNames, namingContext.Tags);
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
