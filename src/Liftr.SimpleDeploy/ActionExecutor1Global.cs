//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Hosting.Contracts;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Liftr.SimpleDeploy
{
    public sealed partial class ActionExecutor : IHostedService
    {
        private async Task ManageGlobalResourcesAsync(HostingEnvironmentOptions targetOptions, KeyVaultClient kvClient, LiftrAzureFactory azFactory)
        {
            var infra = new InfrastructureV2(azFactory, kvClient, _logger);
            var globalRGName = _globalNamingContext.ResourceGroupName(targetOptions.Global.BaseName);
            File.WriteAllText("global-vault-name.txt", _globalNamingContext.KeyVaultName(targetOptions.Global.BaseName));

            if (targetOptions.IPPerRegion > 0)
            {
                await _ipPool.ProvisionIPPoolAsync(targetOptions.Global.Location, targetOptions.IPPerRegion, _globalNamingContext.Tags, targetOptions.Regions);
            }

            var globalResources = await infra.CreateOrUpdateGlobalRGAsync(
                targetOptions.Global.BaseName,
                _globalNamingContext,
                targetOptions.DomainName,
                targetOptions.Global.AddGlobalDB,
                targetOptions.Global.CreateGlobalDBWithZoneRedundancy,
                _hostingOptions.SecretPrefix,
                targetOptions.PartnerCredentialUpdateConfig,
                targetOptions.LogAnalyticsWorkspaceId);

            if (SimpleDeployExtension.AfterProvisionGlobalResourcesAsync != null)
            {
                using (_logger.StartTimedOperation(nameof(SimpleDeployExtension.AfterProvisionGlobalResourcesAsync)))
                {
                    var parameters = new GlobalCallbackParameters()
                    {
                        CallbackConfigurations = _callBackConfigs,
                        BaseName = targetOptions.Global.BaseName,
                        NamingContext = _globalNamingContext,
                        Resources = globalResources,
                        IPPoolManager = _ipPool,
                    };

                    await SimpleDeployExtension.AfterProvisionGlobalResourcesAsync.Invoke(parameters);
                }
            }

            _logger.Information("-----------------------------------------------------------------------");
            _logger.Information($"Successfully finished managing global resources.");
            _logger.Information("-----------------------------------------------------------------------");
        }
    }
}
