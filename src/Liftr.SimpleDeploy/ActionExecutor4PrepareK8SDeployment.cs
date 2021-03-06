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

            var inboundIP = await WriteReservedInboundIPToDiskAsync(azFactory, aksRGName, aksName, parsedRegionInfo.AKSRegion);

            var regionalSubdomain = $"{regionalNamingContext.Location.ShortName()}.{targetOptions.DomainName}";
            File.WriteAllText("aks-domain.txt", $"{aksName}.{targetOptions.DomainName}");
            File.WriteAllText("domain-name.txt", targetOptions.DomainName);
            File.WriteAllText("regional-domain-name.txt", regionalSubdomain);
            File.WriteAllText("aks-name.txt", aksName);
            File.WriteAllText("aks-rg.txt", aksRGName);
            File.WriteAllText("aks-kv.txt", kv.VaultUri);
            File.WriteAllText("vault-name.txt", kv.Name);

            if (SimpleDeployExtension.AfterPrepareK8SDeploymentAsync != null)
            {
                using (_logger.StartTimedOperation(nameof(SimpleDeployExtension.AfterPrepareK8SDeploymentAsync)))
                {
                    var parameters = new PrepareAKSCallbackParameters()
                    {
                        RegionOptions = parsedRegionInfo,
                        CallbackConfigurations = _callBackConfigs,
                        BaseName = parsedRegionInfo.RegionOptions.ComputeBaseName,
                        NamingContext = regionalNamingContext,
                        IPPoolManager = _ipPool,
                        AKSInboundIP = inboundIP,
                    };

                    await SimpleDeployExtension.AfterPrepareK8SDeploymentAsync.Invoke(parameters);
                }
            }

            _logger.Information("Successfully finished k8s deployment preparation work.");
        }
    }
}
