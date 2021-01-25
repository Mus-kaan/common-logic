//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public partial class InfrastructureV2
    {
        public async Task<BaseProvisionedRegionalDataResources> CreateOrUpdateDataRegionAsync(
            string baseName,
            NamingContext namingContext,
            RegionalDataOptions dataOptions,
            bool createVNet)
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

            BaseProvisionedRegionalDataResources provisionedResources = new BaseProvisionedRegionalDataResources();
            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            var rgName = namingContext.ResourceGroupName(baseName);
            var trafficManagerName = namingContext.TrafficManagerName(baseName);
            var cosmosName = namingContext.CosmosDBName(baseName);

            var dnsZoneId = new ResourceId(dataOptions.DNSZoneId);
            provisionedResources.DnsZone = await liftrAzure.GetDNSZoneAsync(dnsZoneId.ResourceGroup, dnsZoneId.ResourceName);
            if (provisionedResources.DnsZone == null)
            {
                provisionedResources.DnsZone = await liftrAzure.CreateDNSZoneAsync(dnsZoneId.ResourceGroup, dnsZoneId.ResourceName, namingContext.Tags);
            }

            var rg = await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
            provisionedResources.ResourceGroup = rg;

            ISubnet subnet = null;
            if (createVNet)
            {
                var vnetName = namingContext.NetworkName(baseName);
                var nsgName = $"{vnetName}-default-nsg";
                var nsg = await liftrAzure.GetOrCreateDefaultNSGAsync(namingContext.Location, rgName, nsgName, namingContext.Tags);
                provisionedResources.VNet = await liftrAzure.GetOrCreateVNetAsync(namingContext.Location, rgName, vnetName, namingContext.Tags, nsg.Id);
                await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(provisionedResources.VNet, dataOptions.LogAnalyticsWorkspaceId);

                provisionedResources.VNet = await provisionedResources.VNet.RemoveEmptySubnetsAsync(_logger);
            }

            if (dataOptions.EnableVNet)
            {
                subnet = provisionedResources.VNet.Subnets[liftrAzure.DefaultSubnetName];
            }

            provisionedResources.TrafficManager = await liftrAzure.GetOrCreateTrafficManagerAsync(rgName, trafficManagerName, namingContext.Tags);
            await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(provisionedResources.TrafficManager, dataOptions.LogAnalyticsWorkspaceId);

            var globalTMId = new ResourceId(dataOptions.GlobalTrafficManagerResourceId);
            var globalTM = await liftrAzure.GetOrCreateTrafficManagerAsync(globalTMId.ResourceGroup, globalTMId.ResourceName, namingContext.Tags);

            await globalTM.WithTrafficManagerEndpointAsync(liftrAzure, provisionedResources.TrafficManager, namingContext.Location, _logger);

            _logger.Information("Set DNS zone '{dnsZone}' CNAME '{cname}' to Traffic Manager '{tmFqdn}'.", provisionedResources.DnsZone.Id, namingContext.Location.ShortName(), provisionedResources.TrafficManager.Fqdn);
            await provisionedResources.DnsZone.Update()
                .DefineCNameRecordSet(namingContext.Location.ShortName())
                .WithAlias(provisionedResources.TrafficManager.Fqdn).WithTimeToLive(600)
                .Attach()
                .DefineCNameRecordSet($"*.{namingContext.Location.ShortName()}")
                .WithAlias(provisionedResources.TrafficManager.Fqdn).WithTimeToLive(600)
                .Attach()
                .ApplyAsync();

            if (dataOptions.DBSupport)
            {
                var dbId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosName}";
                provisionedResources.CosmosDBAccount = await liftrAzure.GetCosmosDBAsync(dbId);
                if (provisionedResources.CosmosDBAccount == null)
                {
                    (var createdDb, _) = await liftrAzure.CreateCosmosDBAsync(namingContext.Location, rgName, cosmosName, namingContext.Tags, subnet);
                    provisionedResources.CosmosDBAccount = createdDb;
                    await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(provisionedResources.CosmosDBAccount, dataOptions.LogAnalyticsWorkspaceId);
                    _logger.Information("Created CosmosDB with Id {ResourceId}", provisionedResources.CosmosDBAccount.Id);
                }
                else
                {
                    _logger.Information("Use existing CosmosDB with Id {ResourceId}", provisionedResources.CosmosDBAccount.Id);
                }
            }
            else
            {
                _logger.Information("Skip creating cosmos db.");
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

            return provisionedResources;
        }
    }
}
