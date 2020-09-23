//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Hosting.Contracts;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Liftr.SimpleDeploy
{
    public sealed partial class ActionExecutor : IHostedService
    {
        private async Task PrepareK8SDeploymentAsync(HostingEnvironmentOptions targetOptions, KeyVaultClient kvClient, LiftrAzureFactory azFactory)
        {
            var liftrAzure = azFactory.GenerateLiftrAzure();
            var infra = new InfrastructureV2(azFactory, kvClient, _logger);
            var globalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, targetOptions.Global.Location);
            var globalRGName = globalNamingContext.ResourceGroupName(targetOptions.Global.BaseName);
            File.WriteAllText("global-vault-name.txt", globalNamingContext.KeyVaultName(targetOptions.Global.BaseName));

            IPPoolManager ipPool = null;
            if (targetOptions.IPPerRegion > 0)
            {
                var ipNamePrefix = globalNamingContext.GenerateCommonName(targetOptions.Global.BaseName, noRegion: true);
                var poolRG = ipNamePrefix + "-ip-pool-rg";
                ipPool = new IPPoolManager(poolRG, ipNamePrefix, azFactory, _logger);
            }

            var parsedRegionInfo = GetRegionalOptions(targetOptions);
            var regionOptions = parsedRegionInfo.RegionOptions;
            var regionalNamingContext = parsedRegionInfo.RegionNamingContext;
            var aksRGName = parsedRegionInfo.AKSRGName;
            var aksName = parsedRegionInfo.AKSName;

            var kv = await GetRegionalKeyVaultAsync(targetOptions, azFactory);

            if (kv == null)
            {
                var errMsg = "Cannot find regional key vault.";
                _logger.Fatal(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            await WriteReservedIPToDiskAsync(azFactory, aksRGName, aksName, parsedRegionInfo.AKSRegion, targetOptions, ipPool);

            var regionalSubdomain = $"{regionalNamingContext.Location.ShortName()}.{targetOptions.DomainName}";
            File.WriteAllText("aks-domain.txt", $"{aksName}.{targetOptions.DomainName}");
            File.WriteAllText("domain-name.txt", targetOptions.DomainName);
            File.WriteAllText("regional-domain-name.txt", regionalSubdomain);
            File.WriteAllText("aks-name.txt", aksName);
            File.WriteAllText("aks-rg.txt", aksRGName);
            File.WriteAllText("aks-kv.txt", kv.VaultUri);
            File.WriteAllText("vault-name.txt", kv.Name);

            if (string.IsNullOrEmpty(targetOptions.DiagnosticsStorageId))
            {
                targetOptions.DiagnosticsStorageId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{globalRGName}/providers/Microsoft.Storage/storageAccounts/{globalNamingContext.StorageAccountName(targetOptions.Global.BaseName)}";
            }

            await GetDiagnosticsStorageAccountAsync(azFactory, targetOptions.DiagnosticsStorageId);

            _logger.Information("Successfully finished k8s deployment preparation work.");
        }
    }
}
