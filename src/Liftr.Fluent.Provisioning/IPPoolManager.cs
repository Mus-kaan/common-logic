//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
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
        public const string c_reservedNamePart = "rsvd-pip";
        private readonly string _ipPoolRG;
        private readonly string _namePrefix;
        private readonly ILiftrAzureFactory _azureClientFactory;
        private readonly ILogger _logger;

        public IPPoolManager(
            string ipPoolRG,
            string namePrefix,
            ILiftrAzureFactory azureClientFactory,
            ILogger logger)
        {
            if (string.IsNullOrEmpty(ipPoolRG))
            {
                throw new ArgumentNullException(nameof(ipPoolRG));
            }

            if (string.IsNullOrEmpty(namePrefix))
            {
                throw new ArgumentNullException(nameof(namePrefix));
            }

            _ipPoolRG = ipPoolRG;
            _namePrefix = namePrefix;
            _azureClientFactory = azureClientFactory ?? throw new ArgumentNullException(nameof(azureClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProvisionIPPoolAsync(
            Region poolLocation,
            int ipPerRegion,
            IEnumerable<Region> regions,
            IDictionary<string, string> tags)
        {
            if (regions == null || !regions.Any())
            {
                throw new ArgumentNullException(nameof(regions));
            }

            using var ops = _logger.StartTimedOperation(nameof(ProvisionIPPoolAsync));
            ops.SetContextProperty(nameof(ipPerRegion), ipPerRegion);

            int createdCount = 0;
            try
            {
                var az = _azureClientFactory.GenerateLiftrAzure();
                var rg = await az.GetOrCreateResourceGroupAsync(poolLocation, _ipPoolRG, tags);

                foreach (var region in regions)
                {
                    var ipNamePrefix = GetIPNamePrefix(region);
                    var existingIPs = await az.ListPublicIPAsync(rg.Name, ipNamePrefix);
                    var currentCount = existingIPs.Count();
                    for (int i = currentCount + 1; i <= ipPerRegion; i++)
                    {
                        var pip = await az.GetOrCreatePublicIPAsync(region, rg.Name, GetIPName(region, i), tags);
                        _logger.Information("Created public IP: {pipAddress}", pip.Inner.IpAddress);
                        createdCount++;
                    }
                }

                _logger.Information("Created {createdCount} IP addresses in the IP pool resource group '{rgId}'.", createdCount, rg.Id);
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<IPublicIPAddress>> ListAllIPAsync(Region location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var az = _azureClientFactory.GenerateLiftrAzure();
            var ipNamePrefix = GetIPNamePrefix(location);
            var existingIPs = await az.ListPublicIPAsync(_ipPoolRG, ipNamePrefix);
            return existingIPs;
        }

        public async Task<IPublicIPAddress> GetAvailableIPAsync(Region location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var az = _azureClientFactory.GenerateLiftrAzure();
            var ipNamePrefix = GetIPNamePrefix(location);
            var existingIPs = await az.ListPublicIPAsync(_ipPoolRG, ipNamePrefix);
            var availableIPs = existingIPs.Where(ip => !ip.HasAssignedLoadBalancer && !ip.HasAssignedNetworkInterface);
            _logger.Information("In the IP pool with name '{rgName}', there are '{existingIPCount}' IPs in region {location}, '{availableIPCount}' IPs are available.", _ipPoolRG, existingIPs.Count(), location.Name, availableIPs.Count());

            var pip = availableIPs.OrderBy(ip => ip.Name).FirstOrDefault();
            return pip;
        }

        private string GetIPNamePrefix(Region location)
        {
            return $"{_namePrefix}-{c_reservedNamePart}-{location.ShortName()}-";
        }

        private string GetIPName(Region location, int cnt)
        {
            return $"{GetIPNamePrefix(location)}{cnt.ToString("d2", CultureInfo.InvariantCulture)}";
        }
    }
}
