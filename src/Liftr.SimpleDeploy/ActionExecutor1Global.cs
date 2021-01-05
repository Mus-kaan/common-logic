﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Hosting.Contracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.SimpleDeploy
{
    public sealed partial class ActionExecutor : IHostedService
    {
        private async Task ManageGlobalResourcesAsync(HostingEnvironmentOptions targetOptions, KeyVaultClient kvClient, LiftrAzureFactory azFactory)
        {
            var infra = new InfrastructureV2(azFactory, kvClient, _logger);
            IPPoolManager ipPool = null;
            var globalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, targetOptions.Global.Location);
            var globalRGName = globalNamingContext.ResourceGroupName(targetOptions.Global.BaseName);
            File.WriteAllText("global-vault-name.txt", globalNamingContext.KeyVaultName(targetOptions.Global.BaseName));

            if (targetOptions.IPPerRegion > 0)
            {
                var ipNamePrefix = globalNamingContext.GenerateCommonName(targetOptions.Global.BaseName, noRegion: true);

                ipPool = new IPPoolManager(ipNamePrefix, azFactory, _logger);

                await ipPool.ProvisionIPPoolAsync(targetOptions.Global.Location, targetOptions.IPPerRegion, globalNamingContext.Tags, targetOptions.IsAKS, targetOptions.Regions);
            }

            var globalResources = await infra.CreateOrUpdateGlobalRGAsync(
                targetOptions.Global.BaseName,
                globalNamingContext,
                targetOptions.DomainName,
                targetOptions.Global.AddGlobalDB,
                targetOptions.LogAnalyticsWorkspaceId);

            if (SimpleDeployExtension.AfterProvisionGlobalResourcesAsync != null)
            {
                using (_logger.StartTimedOperation(nameof(SimpleDeployExtension.AfterProvisionGlobalResourcesAsync)))
                {
                    var parameters = new GlobalCallbackParameters()
                    {
                        CallbackConfigurations = _callBackConfigs,
                        BaseName = targetOptions.Global.BaseName,
                        NamingContext = globalNamingContext,
                        Resources = globalResources,
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
