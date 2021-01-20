//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Hosting.Contracts;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Liftr.SimpleDeploy
{
    public sealed partial class ActionExecutor : IHostedService
    {
        private async Task ManageDataResourcesAsync(
            HostingEnvironmentOptions targetOptions,
            KeyVaultClient kvClient,
            LiftrAzureFactory azFactory,
            string allowedAcisExtensions)
        {
            var liftrAzure = azFactory.GenerateLiftrAzure();
            var infra = new InfrastructureV2(azFactory, kvClient, _logger);
            var globalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, targetOptions.Global.Location);
            var globalRGName = globalNamingContext.ResourceGroupName(targetOptions.Global.BaseName);
            File.WriteAllText("global-vault-name.txt", globalNamingContext.KeyVaultName(targetOptions.Global.BaseName));

            var parsedRegionInfo = GetRegionalOptions(targetOptions);
            var regionOptions = parsedRegionInfo.RegionOptions;
            var regionalNamingContext = parsedRegionInfo.RegionNamingContext;
            var aksRGName = parsedRegionInfo.AKSRGName;
            var aksName = parsedRegionInfo.AKSName;
            regionalNamingContext.Tags["GlobalRG"] = globalRGName;

            regionalNamingContext.Tags["DataRG"] = regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName);

            var dataOptions = new RegionalDataOptions()
            {
                ActiveDBKeyName = _commandOptions.ActiveKeyName,
                SecretPrefix = _hostingOptions.SecretPrefix,
                OneCertCertificates = targetOptions.OneCertCertificates,
                DataPlaneSubscriptions = regionOptions.DataPlaneSubscriptions,
                DataPlaneStorageCountPerSubscription = _hostingOptions.StorageCountPerDataPlaneSubscription,
                EnableVNet = targetOptions.EnableVNet,
                DBSupport = _hostingOptions.DBSupport,
                GlobalKeyVaultResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.KeyVault/vaults/{globalNamingContext.KeyVaultName(targetOptions.Global.BaseName)}",
                GlobalStorageResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.Storage/storageAccounts/{globalNamingContext.StorageAccountName(targetOptions.Global.BaseName)}",
                GlobalCosmosDBResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.DocumentDB/databaseAccounts/{globalNamingContext.CosmosDBName(targetOptions.Global.BaseName)}",
                GlobalTrafficManagerResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.Network/trafficmanagerprofiles/{globalNamingContext.TrafficManagerName(targetOptions.Global.BaseName)}",
                LogAnalyticsWorkspaceId = targetOptions.LogAnalyticsWorkspaceId,
                DomainName = targetOptions.DomainName,
                DNSZoneId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{globalRGName}/providers/Microsoft.Network/dnszones/{targetOptions.DomainName}",
            };

            bool createVNet = targetOptions.IsAKS ? targetOptions.EnableVNet : true;

            if (regionOptions.IsSeparatedDataAndComputeRegion)
            {
                var dataResources = await infra.CreateOrUpdateDataRegionAsync(regionOptions.DataBaseName, regionalNamingContext, dataOptions, createVNet);

                if (SimpleDeployExtension.AfterProvisionDataRegionAsync != null)
                {
                    using (_logger.StartTimedOperation(nameof(SimpleDeployExtension.AfterProvisionRegionalDataResourcesAsync)))
                    {
                        var parameters = new DataRegionCallbackParameters()
                        {
                            CallbackConfigurations = _callBackConfigs,
                            BaseName = regionOptions.DataBaseName,
                            NamingContext = regionalNamingContext,
                            DataOptions = dataOptions,
                            RegionOptions = regionOptions,
                            Resources = dataResources,
                        };

                        await SimpleDeployExtension.AfterProvisionDataRegionAsync.Invoke(parameters);
                    }
                }
            }
            else
            {
                var dataResources = await infra.CreateOrUpdateRegionalDataRGAsync(regionOptions.DataBaseName, regionalNamingContext, dataOptions, createVNet, allowedAcisExtensions);

                if (SimpleDeployExtension.AfterProvisionRegionalDataResourcesAsync != null)
                {
                    using (_logger.StartTimedOperation(nameof(SimpleDeployExtension.AfterProvisionRegionalDataResourcesAsync)))
                    {
                        var parameters = new RegionalDataCallbackParameters()
                        {
                            CallbackConfigurations = _callBackConfigs,
                            BaseName = regionOptions.DataBaseName,
                            NamingContext = regionalNamingContext,
                            DataOptions = dataOptions,
                            RegionOptions = regionOptions,
                            Resources = dataResources,
                        };

                        await SimpleDeployExtension.AfterProvisionRegionalDataResourcesAsync.Invoke(parameters);
                    }
                }
            }

            _logger.Information("-----------------------------------------------------------------------");
            _logger.Information($"Successfully finished managing data resources.");
            _logger.Information("-----------------------------------------------------------------------");
        }
    }
}
