//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent;
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
        private async Task UpdateTrafficRoutingAsync(HostingEnvironmentOptions targetOptions, KeyVaultClient kvClient, LiftrAzureFactory azFactory)
        {
            if (_commandOptions.Action == ActionType.CreateOrUpdateGlobal ||
                _commandOptions.Action == ActionType.CreateOrUpdateRegionalData)
            {
                return;
            }

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
            var aksRegion = parsedRegionInfo.AKSRegion;
            var computeBaseName = regionOptions.ComputeBaseName;

            if ((_commandOptions.Action == ActionType.UpdateComputeIPInTrafficManager && targetOptions.IsAKS) ||
                (_commandOptions.Action == ActionType.CreateOrUpdateRegionalCompute && targetOptions.IsAKS) ||
                _commandOptions.Action == ActionType.PrepareK8SAppDeployment)
            {
                var az = liftrAzure.FluentClient;
                var tmId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName)}/providers/Microsoft.Network/trafficmanagerprofiles/{regionalNamingContext.TrafficManagerName(regionOptions.DataBaseName)}";

                var dnsZone = await liftrAzure.GetDNSZoneAsync(globalRGName, targetOptions.DomainName);
                if (dnsZone == null)
                {
                    var errMsg = $"Cannot find the DNS zone for domina '{targetOptions.DomainName}' in RG '{globalRGName}'.";
                    _logger.Error(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                var aksHelper = new AKSNetworkHelper(_logger);
                var inboundIP = await aksHelper.GetAKSInboundIPAsync(liftrAzure, aksRGName, aksName, aksRegion);
                if (inboundIP == null)
                {
                    var errMsg = $"Cannot find the public Ip address for the AKS cluster. aksRGName:{aksRGName}, aksName:{aksName}, region:{aksRegion}.";
                    _logger.Warning(errMsg);

                    if (_commandOptions.Action == ActionType.UpdateComputeIPInTrafficManager)
                    {
                        throw new InvalidOperationException(errMsg);
                    }
                }
                else
                {
                    _logger.Information("Find the IP of the AKS is: {IPAddress}", inboundIP.IPAddress);

                    if (string.IsNullOrEmpty(inboundIP.IPAddress))
                    {
                        _logger.Error("The IP address is null of the created Pulic IP with Id {PipResourceId}", inboundIP.Id);
                        throw new InvalidOperationException($"The IP address is null of the created Pulic IP with Id {inboundIP.Id}");
                    }

                    await dnsZone.Update().DefineARecordSet(aksName).WithIPv4Address(inboundIP.IPAddress).WithTimeToLive(60).Attach().ApplyAsync();
                    await dnsZone.Update().DefineARecordSet("*." + aksName).WithIPv4Address(inboundIP.IPAddress).WithTimeToLive(60).Attach().ApplyAsync();
                    await dnsZone.Update().DefineARecordSet("thanos-0-" + aksName).WithIPv4Address(inboundIP.IPAddress).WithTimeToLive(60).Attach().ApplyAsync();
                    await dnsZone.Update().DefineARecordSet("thanos-1-" + aksName).WithIPv4Address(inboundIP.IPAddress).WithTimeToLive(60).Attach().ApplyAsync();
                    _logger.Information("Successfully added DNS A record '{recordName}' to IP '{ipAddress}'.", aksName, inboundIP.IPAddress);

                    if (_commandOptions.Action == ActionType.UpdateComputeIPInTrafficManager)
                    {
                        var epName = $"{aksRGName}-{SdkContext.RandomResourceName(string.Empty, 5).Substring(0, 3)}";
                        _logger.Information("New endpoint name: {epName}", epName);
                        await aksHelper.AddPulicIpToTrafficManagerAsync(az, tmId, epName, inboundIP.IPAddress, enabled: true);
                        _logger.Information("Successfully updated AKS public IP in the traffic manager.");
                    }
                }
            }
            else if (_commandOptions.Action == ActionType.UpdateComputeIPInTrafficManager && !targetOptions.IsAKS)
            {
                var az = liftrAzure.FluentClient;
                var tmId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName)}/providers/Microsoft.Network/trafficmanagerprofiles/{regionalNamingContext.TrafficManagerName(regionOptions.DataBaseName)}";

                var dnsZone = await liftrAzure.GetDNSZoneAsync(globalRGName, targetOptions.DomainName);
                if (dnsZone == null)
                {
                    var errMsg = $"Cannot find the DNS zone for domina '{targetOptions.DomainName}' in RG '{globalRGName}'.";
                    _logger.Error(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                var vmssResources = await infra.GetRegionalVMSSResourcesAsync(regionalNamingContext, computeBaseName);

                var epName = $"{aksRGName}-{SdkContext.RandomResourceName(string.Empty, 5).Substring(0, 3)}";
                await NetworkHelper.AddPulicIpToTrafficManagerAsync(az, tmId, epName, vmssResources.pip.IPAddress, enabled: true, logger: _logger);
                _logger.Information("Successfully updated VMSS public IP in the traffic manager.");
            }
        }
    }
}
