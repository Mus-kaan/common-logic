//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public partial class InfrastructureV2
    {
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

            _logger.Information("Start adding access policy for managed identity to regional kv.");
            provisionedResources.KeyVault = await provisionedResources.KeyVault.RefreshAsync();
            await provisionedResources.KeyVault.Update()
                .DefineAccessPolicy()
                .ForObjectId(provisionedResources.ManagedIdentity.GetObjectId())
                .AllowKeyPermissions(KeyPermissions.Get, KeyPermissions.List)
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

                if (!string.IsNullOrEmpty(dataOptions.GlobalStorageResourceId))
                {
                    var storId = new ResourceId(dataOptions.GlobalStorageResourceId);
                    var gblStor = await liftrAzure.GetStorageAccountAsync(storId.ResourceGroup, storId.ResourceName);
                    if (gblStor == null)
                    {
                        throw new InvalidOperationException("Cannot find the global storage account with Id: " + dataOptions.GlobalStorageResourceId);
                    }

                    rpAssets.GlobalStorageAccountName = gblStor.Name;
                }

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
                provisionedResources.RPAssetOptions = rpAssets;

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
    }
}
