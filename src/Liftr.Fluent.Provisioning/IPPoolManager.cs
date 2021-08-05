//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Hosting.Contracts;
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
        private const string c_resourceGroupSufix = "ip-pool-rg";
        private readonly bool _isAKS;
        private readonly string _namePrefix;
        private readonly ILiftrAzureFactory _azureClientFactory;
        private readonly Serilog.ILogger _logger;

        /// <summary>
        /// There will be two IP pools for AKS hosting, one for inbound IP another for outbound IP.
        /// For VMSS hosting, there is only one IP pool. Both inbound and outbound will share the same IP.
        /// </summary>
        public IPPoolManager(
            string namePrefix,
            bool isAKS,
            ILiftrAzureFactory azureClientFactory,
            Serilog.ILogger logger)
        {
            if (string.IsNullOrEmpty(namePrefix))
            {
                throw new ArgumentNullException(nameof(namePrefix));
            }

            _isAKS = isAKS;
            _namePrefix = namePrefix;
            _azureClientFactory = azureClientFactory ?? throw new ArgumentNullException(nameof(azureClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (_isAKS)
            {
                OutboundIPPoolResourceGroupName = GetResourceGroupName(namePrefix, IPCategory.Outbound);
                InboundIPPoolResourceGroupName = GetResourceGroupName(namePrefix, IPCategory.Inbound);
            }
            else
            {
                OutboundIPPoolResourceGroupName = GetResourceGroupName(namePrefix, IPCategory.InOutbound);
                InboundIPPoolResourceGroupName = OutboundIPPoolResourceGroupName;
            }
        }

        public string OutboundIPPoolResourceGroupName { get; }

        public string InboundIPPoolResourceGroupName { get; }

        public async Task ProvisionIPPoolAsync(
            Region poolLocation,
            int ipPerRegion,
            IDictionary<string, string> tags,
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
                var az = _azureClientFactory.GenerateLiftrAzure();
                IResourceGroup rg = null, inboundRG = null, outboundRG = null;

                if (_isAKS)
                {
                    inboundRG = await az.GetOrCreateResourceGroupAsync(poolLocation, InboundIPPoolResourceGroupName, tags);
                    outboundRG = await az.GetOrCreateResourceGroupAsync(poolLocation, OutboundIPPoolResourceGroupName, tags);
                }
                else
                {
                    rg = await az.GetOrCreateResourceGroupAsync(poolLocation, OutboundIPPoolResourceGroupName, tags);
                }

                foreach (var regionOption in regionOptions)
                {
                    var region = regionOption.Location;

                    if (_isAKS)
                    {
                        // AKS has separated inbound and outbound IP pool.
                        await ProvisionIPPoolAsync(region, ipPerRegion, tags, inboundRG, PublicIPSkuType.Standard, az, IPCategory.Inbound);
                        await ProvisionIPPoolAsync(region, ipPerRegion, tags, outboundRG, PublicIPSkuType.Standard, az, IPCategory.Outbound);
                    }
                    else
                    {
                        await ProvisionIPPoolAsync(region, ipPerRegion, tags, rg, PublicIPSkuType.Standard, az, IPCategory.InOutbound);
                    }
                }
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public Task<IEnumerable<IPublicIPAddress>> ListOutboundIPAsync(Region location)
        {
            if (_isAKS)
            {
                return ListIPAsync(location, IPCategory.Outbound);
            }
            else
            {
                return ListIPAsync(location, IPCategory.InOutbound);
            }
        }

        public Task<IEnumerable<IPublicIPAddress>> ListInboundIPAsync(Region location)
        {
            if (_isAKS)
            {
                return ListIPAsync(location, IPCategory.Inbound);
            }
            else
            {
                return ListIPAsync(location, IPCategory.InOutbound);
            }
        }

        public Task<IPublicIPAddress> GetAvailableInboundIPAsync(Region location)
        {
            if (_isAKS)
            {
                return GetAvailableIPAsync(location, IPCategory.Inbound);
            }
            else
            {
                return GetAvailableIPAsync(location, IPCategory.InOutbound);
            }
        }

        public Task<IPublicIPAddress> GetAvailableOutboundIPAsync(Region location)
        {
            if (_isAKS)
            {
                return GetAvailableIPAsync(location, IPCategory.Outbound);
            }
            else
            {
                return GetAvailableIPAsync(location, IPCategory.InOutbound);
            }
        }

        private async Task<IEnumerable<IPublicIPAddress>> ListIPAsync(Region location, IPCategory ipCategory)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var az = _azureClientFactory.GenerateLiftrAzure();
            var ipNamePrefix = GetIPNamePrefix(location, ipCategory);
            IEnumerable<IPublicIPAddress> existingIPs;

            if (ipCategory == IPCategory.Inbound)
            {
                existingIPs = await az.ListPublicIPAsync(InboundIPPoolResourceGroupName, ipNamePrefix);
            }
            else
            {
                existingIPs = await az.ListPublicIPAsync(OutboundIPPoolResourceGroupName, ipNamePrefix);
            }

            return existingIPs;
        }

        private async Task<IPublicIPAddress> GetAvailableIPAsync(Region location, IPCategory ipCategory)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var az = _azureClientFactory.GenerateLiftrAzure();
            var ipNamePrefix = GetIPNamePrefix(location, ipCategory);
            IEnumerable<IPublicIPAddress> existingIPs = null;
            string poolResourceGroupName = string.Empty;

            if (ipCategory == IPCategory.Inbound)
            {
                existingIPs = await az.ListPublicIPAsync(InboundIPPoolResourceGroupName, ipNamePrefix);
                poolResourceGroupName = InboundIPPoolResourceGroupName;
            }
            else
            {
                existingIPs = await az.ListPublicIPAsync(OutboundIPPoolResourceGroupName, ipNamePrefix);
                poolResourceGroupName = OutboundIPPoolResourceGroupName;
            }

            if (existingIPs is null || !existingIPs.Any())
            {
                _logger.Information("In the IP pool with name '{rgName}', there are '{existingIPCount}' IPs in region {location}", poolResourceGroupName, existingIPs?.Count(), location.Name);
                return null;
            }

            var availableIPs = existingIPs.Where(ip => !ip.HasAssignedLoadBalancer && !ip.HasAssignedNetworkInterface);
            _logger.Information("In the IP pool with name '{rgName}', there are '{existingIPCount}' IPs in region {location}, '{availableIPCount}' IPs are available.", poolResourceGroupName, existingIPs?.Count(), location.Name, availableIPs?.Count());

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
            IPCategory ipCategory)
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

        private string GetIPNamePrefix(Region location, IPCategory ipCategory)
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

        private string GetResourceGroupName(string namePrefix, IPCategory ipcategory)
        {
            string delimiter = "-";
            string rgName;

            if (ipcategory != IPCategory.InOutbound)
            {
                rgName = $"{namePrefix}{delimiter}{ipcategory}{delimiter}{c_resourceGroupSufix}";
            }
            else
            {
                rgName = $"{namePrefix}{delimiter}{c_resourceGroupSufix}";
            }

            return rgName;
        }
    }
}
