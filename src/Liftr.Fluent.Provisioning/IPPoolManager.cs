//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Hosting.Contracts;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class IPPoolManager
    {
        public const string c_reservedNamePart = "rsvd-stdpip";
        private readonly string _ipPoolRG;
        private readonly string _inboundPoolRG;
        private readonly string _outboundPoolRG;
        private readonly string _namePrefix;
        private readonly ILiftrAzureFactory _azureClientFactory;
        private readonly ILogger _logger;

        public IPPoolManager(
            string namePrefix,
            ILiftrAzureFactory azureClientFactory,
            ILogger logger)
        {
            if (string.IsNullOrEmpty(namePrefix))
            {
                throw new ArgumentNullException(nameof(namePrefix));
            }

            _ipPoolRG = GetResourceGroupName(namePrefix, "ip-pool-rg");
            _inboundPoolRG = GetResourceGroupName(namePrefix, "ip-pool-rg", IPCategory.Inbound);
            _outboundPoolRG = GetResourceGroupName(namePrefix, "ip-pool-rg", IPCategory.Outbound);
            _namePrefix = namePrefix;
            _azureClientFactory = azureClientFactory ?? throw new ArgumentNullException(nameof(azureClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProvisionIPPoolAsync(
            Region poolLocation,
            int ipPerRegion,
            IDictionary<string, string> tags,
            bool isAKS,
            IEnumerable<RegionOptions> regionOptions)
        {
            if (regionOptions == null || !regionOptions.Any())
            {
                throw new ArgumentNullException(nameof(regionOptions));
            }

            using var ops = _logger.StartTimedOperation(nameof(ProvisionIPPoolAsync));
            ops.SetContextProperty(nameof(ipPerRegion), ipPerRegion);

            try
            {
                bool isSeparatedDataAndComputeRegion = regionOptions.First().IsSeparatedDataAndComputeRegion;
                var az = _azureClientFactory.GenerateLiftrAzure();
                var rg = await az.GetOrCreateResourceGroupAsync(poolLocation, _ipPoolRG, tags);
                var inboundRG = await az.GetOrCreateResourceGroupAsync(poolLocation, _inboundPoolRG, tags);
                var outboundRG = await az.GetOrCreateResourceGroupAsync(poolLocation, _outboundPoolRG, tags);
                bool availabilityZoneSupport = false;

                foreach (var regionOption in regionOptions)
                {
                    if (isSeparatedDataAndComputeRegion)
                    {
                        var computeRegionOptions = regionOption.ComputeRegions;
                        foreach (var computeRegionOption in computeRegionOptions)
                        {
                            var computeRegion = computeRegionOption.Location;
                            _logger.Information($"For Compute Region {computeRegion}, IP SKU Type is {PublicIPSkuType.Standard} and AKS hosting is set {isAKS}");

                            if (isAKS)
                            {
                                await ProvisionIPPoolAsync(computeRegion, ipPerRegion, tags, inboundRG, PublicIPSkuType.Standard, az, IPCategory.Inbound);
                                await ProvisionIPPoolAsync(computeRegion, ipPerRegion, tags, outboundRG, PublicIPSkuType.Standard, az, IPCategory.Outbound);
                            }
                            else
                            {
                                await ProvisionIPPoolAsync(computeRegion, ipPerRegion, tags, rg, PublicIPSkuType.Standard, az);
                            }
                        }
                    }
                    else
                    {
                        var region = regionOption.Location;
                        availabilityZoneSupport = regionOption.SupportAvailabilityZone;
                        _logger.Information($"For Region {region}, Availability Zone Support is {availabilityZoneSupport} and IP SKU Type is {PublicIPSkuType.Standard} and AKS hosting is set {isAKS}");

                        if (isAKS)
                        {
                            // Creating 1 inbound IP and 1 Outbound IP for AKS
                            await ProvisionIPPoolAsync(region, ipPerRegion, tags, inboundRG, PublicIPSkuType.Standard, az, IPCategory.Inbound);
                            await ProvisionIPPoolAsync(region, ipPerRegion, tags, outboundRG, PublicIPSkuType.Standard, az, IPCategory.Outbound);
                        }
                        else
                        {
                            await ProvisionIPPoolAsync(region, ipPerRegion, tags, rg, PublicIPSkuType.Standard, az);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<IPublicIPAddress>> ListAllIPAsync(Region location, IPCategory ipCategory = IPCategory.InOutbound)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var az = _azureClientFactory.GenerateLiftrAzure();
            var ipNamePrefix = GetIPNamePrefix(location, ipCategory);
            IEnumerable<IPublicIPAddress> existingIPs = null;

            if (ipCategory == IPCategory.Outbound)
            {
                existingIPs = await az.ListPublicIPAsync(_outboundPoolRG, ipNamePrefix);
            }
            else if (ipCategory == IPCategory.Inbound)
            {
                existingIPs = await az.ListPublicIPAsync(_inboundPoolRG, ipNamePrefix);
            }
            else
            {
                existingIPs = await az.ListPublicIPAsync(_ipPoolRG, ipNamePrefix);
            }

            return existingIPs;
        }

        public async Task<IPublicIPAddress> GetAvailableIPAsync(Region location, IPCategory ipCategory = IPCategory.InOutbound)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var az = _azureClientFactory.GenerateLiftrAzure();
            var ipNamePrefix = GetIPNamePrefix(location, ipCategory);
            IEnumerable<IPublicIPAddress> existingIPs = null;
            string ipPoolRG = string.Empty;

            if (ipCategory == IPCategory.Outbound)
            {
                existingIPs = await az.ListPublicIPAsync(_outboundPoolRG, ipNamePrefix);
                ipPoolRG = _outboundPoolRG;
            }
            else if (ipCategory == IPCategory.Inbound)
            {
                existingIPs = await az.ListPublicIPAsync(_inboundPoolRG, ipNamePrefix);
                ipPoolRG = _inboundPoolRG;
            }
            else
            {
                existingIPs = await az.ListPublicIPAsync(_ipPoolRG, ipNamePrefix);
                ipPoolRG = _ipPoolRG;
            }

            if (existingIPs is null || !existingIPs.Any())
            {
                _logger.Information("In the IP pool with name '{rgName}', there are '{existingIPCount}' IPs in region {location}", ipPoolRG, existingIPs?.Count(), location.Name);
                return null;
            }

            var availableIPs = existingIPs.Where(ip => !ip.HasAssignedLoadBalancer && !ip.HasAssignedNetworkInterface);
            _logger.Information("In the IP pool with name '{rgName}', there are '{existingIPCount}' IPs in region {location}, '{availableIPCount}' IPs are available.", ipPoolRG, existingIPs?.Count(), location.Name, availableIPs?.Count());

            if (availableIPs is null || !availableIPs.Any())
            {
                return null;
            }

            var pip = availableIPs.OrderBy(ip => ip.Name).FirstOrDefault();
            return pip;
        }

        private async Task ProvisionIPPoolAsync(
            Region region,
            int ipPerRegion,
            IDictionary<string, string> tags,
            IResourceGroup rg,
            PublicIPSkuType ipSku,
            ILiftrAzure az,
            IPCategory ipCategory = IPCategory.InOutbound)
        {
            var ipNamePrefix = GetIPNamePrefix(region, ipCategory);
            var existingIPs = await az.ListPublicIPAsync(rg.Name, ipNamePrefix);
            var currentCount = existingIPs.Count();
            var createdCount = 0;
            _logger.Information($"Existing IPs count is {currentCount}");
            for (int i = currentCount + 1; i <= ipPerRegion; i++)
            {
                var pip = await az.GetOrCreatePublicIPAsync(region, rg.Name, GetIPName(region, i, ipNamePrefix), tags, ipSku);
                _logger.Information("Created public IP: {pipAddress}", pip.Inner.IpAddress);
                createdCount++;
            }

            _logger.Information("Created {createdCount} IP addresses in the IP pool resource group '{rgId}'.", createdCount, rg.Id);
        }

        private string GetIPNamePrefix(Region location, IPCategory ipCategory = IPCategory.InOutbound)
        {
            if (ipCategory != IPCategory.InOutbound)
            {
                return $"{_namePrefix}-{GetReservedNamePartWithCategory(ipCategory.ToString())}-{location.ShortName()}-";
            }

            return $"{_namePrefix}-{c_reservedNamePart}-{location.ShortName()}-";
        }

        private static string GetIPName(Region location, int cnt, string ipNamePrefix)
        {
            return $"{ipNamePrefix}{cnt.ToString("d2", CultureInfo.InvariantCulture)}";
        }

        private static string GetReservedNamePartWithCategory(string ipCategory)
        {
            return $"{c_reservedNamePart}-{ipCategory}";
        }

        private string GetResourceGroupName(string namePrefix, string suffix = null, IPCategory ipcategory = IPCategory.InOutbound, string delimiter = "-")
        {
            string rgName = string.Empty;

            if (ipcategory != IPCategory.InOutbound)
            {
                rgName = $"{namePrefix}{delimiter}{ipcategory.ToString()}{delimiter}{suffix}";
            }
            else
            {
                rgName = $"{namePrefix}{delimiter}{suffix}";
            }

            return rgName;
        }
    }
}
