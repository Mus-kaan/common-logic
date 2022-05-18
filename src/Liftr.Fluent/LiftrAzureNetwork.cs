//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.Management.PrivateDns.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Liftr.Fluent.Network;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal partial class LiftrAzure
    {
        public const string c_vnetAddressSpace = "10.66.0.0/16";                // 10.66.0.0 - 10.66.255.255 (65536 addresses)
        public const string c_defaultSubnetAddressSpace = "10.66.255.0/24";     // 10.66.255.0 - 10.66.255.255 (256 addresses)

        public string DefaultSubnetName { get; } = "default";

        public string PrivateEndpointProxySubnetName { get; } = "pe-proxy";

        #region Network
        public async Task<INetworkSecurityGroup> GetOrCreateDefaultNSGAsync(
            Region location,
            string rgName,
            string nsgName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            var nsg = await GetNSGAsync(rgName, nsgName, cancellationToken);

            if (nsg != null)
            {
                _logger.Information("Using existing nsg with id '{nsgId}'.", nsg.Id);
                return nsg;
            }

            nsg = await FluentClient.NetworkSecurityGroups
                .Define(nsgName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .AllowAny80TCPInBound()
                .AllowAny443TCPInBound()
                .WithTags(tags)
                .CreateAsync(cancellationToken);

            _logger.Information("Created default nsg with id '{nsgId}'.", nsg.Id);
            return nsg;
        }

        public Task<INetworkSecurityGroup> GetNSGAsync(string rgName, string nsgName, CancellationToken cancellationToken = default)
        {
            return FluentClient.NetworkSecurityGroups
                .GetByResourceGroupAsync(rgName, nsgName, cancellationToken);
        }

        public async Task<INetwork> GetOrCreateVNetAsync(
            Region location,
            string rgName,
            string vnetName,
            IDictionary<string, string> tags,
            string nsgId = null,
            CancellationToken cancellationToken = default)
        {
            var vnet = await GetVNetAsync(rgName, vnetName, cancellationToken);

            if (vnet == null)
            {
                vnet = await CreateVNetAsync(location, rgName, vnetName, tags, nsgId, cancellationToken);
            }

            return vnet;
        }

        public async Task<INetwork> GetVNetAsync(
            string rgName,
            string vnetName,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Getting VNet. rgName: {rgName}, vnetName: {vnetName} ...", rgName, vnetName);
            var vnet = await FluentClient
                .Networks
                .GetByResourceGroupAsync(rgName, vnetName, cancellationToken);

            if (vnet == null)
            {
                _logger.Information("Cannot find VNet. rgName: {rgName}, vnetName: {vnetName} ...", rgName, vnetName);
            }

            return vnet;
        }

        public async Task<INetwork> CreateVNetAsync(
            Region location,
            string rgName,
            string vnetName,
            IDictionary<string, string> tags,
            string nsgId = null,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Creating vnet with name {vnetName} in {rgName}", vnetName, rgName);

            var temp = FluentClient.Networks
                .Define(vnetName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithAddressSpace(c_vnetAddressSpace)
                .DefineSubnet(DefaultSubnetName)
                .WithAddressPrefix(c_defaultSubnetAddressSpace)
                .WithAccessFromService(ServiceEndpointType.MicrosoftStorage)
                .WithAccessFromService(ServiceEndpointType.MicrosoftAzureCosmosDB)
                .WithAccessFromService(LiftrServiceEndpointType.MicrosoftKeyVault);

            if (!string.IsNullOrEmpty(nsgId))
            {
                temp = temp.WithExistingNetworkSecurityGroup(nsgId);
            }

            var vnet = await temp.Attach().WithTags(tags).CreateAsync(cancellationToken);

            _logger.Information("Created VNet with {resourceId}", vnet.Id);
            return vnet;
        }

        public async Task<ISubnet> CreateNewSubnetAsync(
            INetwork vnet,
            string subnetName,
            string nsgId = null,
            CancellationToken cancellationToken = default)
        {
            if (vnet == null)
            {
                return null;
            }

            var existingSubnets = vnet.Subnets;
            if (existingSubnets == null || existingSubnets.Count == 0)
            {
                var ex = new InvalidOperationException($"To create a new subnet similar to the existing subnets, please make sure there is at least one subnet in the existing vnet '{vnet.Id}'.");
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            _logger.Information("There exist {subnetCount} subnets in the vnet '{vnetId}'.", existingSubnets.Count(), vnet.Id);
            if (existingSubnets.ContainsKey(subnetName))
            {
                return existingSubnets[subnetName];
            }

            var oneSubnetPrefix = existingSubnets.FirstOrDefault().Value.AddressPrefix;

            var nonReservedSubnets = existingSubnets
                .Where(kvp => !kvp.Key.OrdinalEquals(DefaultSubnetName))
                .Where(kvp => !kvp.Key.OrdinalEquals(PrivateEndpointProxySubnetName))
                .Select(kvp => kvp.Value);

            var largestValue = nonReservedSubnets
                .Select(subnet => int.Parse(subnet.AddressPrefix.Split('.')[2], CultureInfo.InvariantCulture))
                .OrderByDescending(i => i)
                .FirstOrDefault();

            var newIPPart = largestValue + 1;
            var parts = oneSubnetPrefix.Split('.');
            parts[2] = newIPPart.ToString(CultureInfo.InvariantCulture);
            var newCIDR = string.Join(".", parts);

            _logger.Information("Adding a new subnet with name {subnetName} and CIDR {subnetCIDR} to vnet '{vnetId}'.", subnetName, newCIDR, vnet.Id);

            var temp = vnet.Update()
                .DefineSubnet(subnetName)
                .WithAddressPrefix(newCIDR)
                .WithAccessFromService(ServiceEndpointType.MicrosoftStorage)
                .WithAccessFromService(ServiceEndpointType.MicrosoftAzureCosmosDB)
                .WithAccessFromService(LiftrServiceEndpointType.MicrosoftKeyVault);

            if (!string.IsNullOrEmpty(nsgId))
            {
                temp = temp.WithExistingNetworkSecurityGroup(nsgId);
            }

            await temp.Attach().ApplyAsync(cancellationToken);
            await vnet.RefreshAsync(cancellationToken);
            return vnet.Subnets[subnetName];
        }

        public async Task<Subnet> CreateIPv6SubnetAsync(
            INetwork vnet,
            string subnetName,
            INetworkSecurityGroup nsg,
            string ipv4AddressPrefix,
            string ipv6AddressPrefix,
            CancellationToken cancellationToken = default)
        {
            if (vnet == null)
            {
                throw new ArgumentNullException(nameof(vnet));
            }

            if (string.IsNullOrEmpty(subnetName))
            {
                throw new ArgumentNullException(nameof(subnetName));
            }

            if (nsg == null)
            {
                throw new ArgumentNullException(nameof(nsg));
            }

            if (string.IsNullOrEmpty(ipv4AddressPrefix))
            {
                throw new ArgumentNullException(nameof(ipv4AddressPrefix));
            }

            if (string.IsNullOrEmpty(ipv6AddressPrefix))
            {
                throw new ArgumentNullException(nameof(ipv6AddressPrefix));
            }

            await IPv6SubnetHelper.CreateIPv6SubnetAsync(this, vnet, subnetName, nsg, ipv4AddressPrefix, ipv6AddressPrefix, cancellationToken);

            return await GetIPv6SubnetAsync(vnet, subnetName, cancellationToken);
        }

        public async Task<ISubnet> GetSubnetAsync(string subnetId, CancellationToken cancellationToken = default)
        {
            var parsedSubnetId = new Liftr.Contracts.ResourceId(subnetId);
            var vnet = await GetVNetAsync(parsedSubnetId.ResourceGroup, parsedSubnetId.ResourceName, cancellationToken);
            if (vnet == null)
            {
                return null;
            }

            if (vnet.Subnets.ContainsKey(parsedSubnetId.ChildResourceName))
            {
                return vnet.Subnets[parsedSubnetId.ChildResourceName];
            }

            return null;
        }

        public async Task<IPrivateDnsZone> CreateNewPrivateDNSZoneAsync(LiftrAzure liftrAzure, string name, string linkName, string rg, PrivateEndpoint privateEndpoint, string vnet, CancellationToken cancellationToken = default)
        {
            var ipv4Address = await PrivateEndpointHelper.GetIPAddressAsync(liftrAzure, rg, privateEndpoint);
            var privateDnsZone = await FluentClient.PrivateDnsZones.Define(name).
                                                    WithExistingResourceGroup(rg).
                                                    DefineARecordSet("*").
                                                    WithIPv4Address(ipv4Address).
                                                    Attach().
                                                    CreateAsync(cancellationToken);

            privateDnsZone = await privateDnsZone.Update().
                                DefineVirtualNetworkLink(linkName).
                                EnableAutoRegistration().
                                WithReferencedVirtualNetworkId(vnet).
                                Attach().
                                ApplyAsync(cancellationToken);

            return privateDnsZone;
        }

        public Task<PrivateEndpoint> CreatePrivateEndpointAsync(ILiftrAzure liftrAzure, string name, string rg, string subnet, string location, string privateLinkServiceId, CancellationToken cancellationToken = default)
        {
            return PrivateEndpointHelper.CreatePrivateEndpointAsync(liftrAzure, name, rg, subnet, location, privateLinkServiceId, cancellationToken);
        }

        public Task<Subnet> GetIPv6SubnetAsync(INetwork vnet, string subnetName, CancellationToken cancellationToken = default)
        {
            return IPv6SubnetHelper.GetSubnetAsync(this, vnet, subnetName, cancellationToken);
        }

        public async Task<IPublicIPAddress> GetOrCreatePublicIPAsync(
            Region location,
            string rgName,
            string pipName,
            IDictionary<string, string> tags,
            PublicIPSkuType skuType = null,
            CancellationToken cancellationToken = default)
        {
            var pip = await GetPublicIPAsync(rgName, pipName, cancellationToken);
            if (pip == null)
            {
                pip = await CreatePublicIPAsync(location, rgName, pipName, tags, skuType, cancellationToken);
            }

            return pip;
        }

        public async Task<IPublicIPAddress> GetOrCreatePublicIPv6Async(
            Region location,
            string rgName,
            string pipName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            var pip = await GetPublicIPAsync(rgName, pipName, cancellationToken);
            if (pip == null)
            {
                pip = await CreatePublicIPv6Async(location, rgName, pipName, tags, cancellationToken);
            }

            return pip;
        }

        public async Task<IPublicIPAddress> CreatePublicIPAsync(
            Region location,
            string rgName,
            string pipName,
            IDictionary<string, string> tags,
            PublicIPSkuType skuType = null,
            CancellationToken cancellationToken = default)
        {
            if (skuType == null)
            {
                skuType = PublicIPSkuType.Basic;
            }

            _logger.Information("Start creating Public IP address with SKU: {skuType} with name: {pipName} in RG: {rgName} ...", skuType, pipName, rgName);

            var pip = await FluentClient
                .PublicIPAddresses
                .Define(pipName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithSku(skuType)
                .WithStaticIP()
                .WithLeafDomainLabel(pipName)
                .WithTags(tags)
                .CreateAsync(cancellationToken);

            _logger.Information("Created Public IP address with resourceId: {resourceId}", pip.Id);
            return pip;
        }

        public async Task<IPublicIPAddress> CreatePublicIPv6Async(
            Region location,
            string rgName,
            string pipName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Start creating Public IPv6 address with name: {pipName} in RG: {rgName} ...", pipName, rgName);

            var helper = new PublicIPv6Helper(_logger);
            await helper.CreatePublicIPv6Async(this, location, rgName, pipName, tags);

            var pip = await GetPublicIPAsync(rgName, pipName, cancellationToken);

            _logger.Information("Created Public IPv6 address with resourceId: {resourceId}", pip.Id);
            return pip;
        }

        public Task<IPublicIPAddress> GetPublicIPAsync(string rgName, string pipName, CancellationToken cancellationToken = default)
        {
            _logger.Information("Start getting Public IP with name: {pipName} ...", pipName);

            return FluentClient
                .PublicIPAddresses
                .GetByResourceGroupAsync(rgName, pipName, cancellationToken);
        }

        public async Task<IEnumerable<IPublicIPAddress>> ListPublicIPAsync(string rgName, string namePrefix = null, CancellationToken cancellationToken = default)
        {
            _logger.Information($"Listing Public IP in resource group {rgName} with prefix {namePrefix} ...");

            IEnumerable<IPublicIPAddress> ips = (await FluentClient
                .PublicIPAddresses
                .ListByResourceGroupAsync(rgName, loadAllPages: true, cancellationToken: cancellationToken)).ToList();

            if (!string.IsNullOrEmpty(namePrefix))
            {
                ips = ips.Where((pip) => pip.Name.OrdinalStartsWith(namePrefix));
            }

            _logger.Information($"Found {ips.Count()} Public IP in resource group {rgName} with prefix {namePrefix}.");

            return ips;
        }

        public async Task<ITrafficManagerProfile> GetOrCreateTrafficManagerAsync(
            string rgName,
            string tmName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            var tm = await GetTrafficManagerAsync(rgName, tmName, cancellationToken);
            if (tm == null)
            {
                tm = await CreateTrafficManagerAsync(rgName, tmName, tags, cancellationToken);
            }

            return tm;
        }

        public async Task<ITrafficManagerProfile> CreateTrafficManagerAsync(
            string rgName,
            string tmName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Creating a Traffic Manager with name {@tmName} ...", tmName);
            var tm = await FluentClient
                .TrafficManagerProfiles
                .Define(tmName)
                .WithExistingResourceGroup(rgName)
                .WithLeafDomainLabel(tmName)
                .WithWeightBasedRouting()
                .DefineExternalTargetEndpoint("default-endpoint")
                    .ToFqdn("40.76.4.15") // microsoft.com
                    .FromRegion(Region.USWest)
                    .WithTrafficDisabled()
                    .Attach()
                .WithHttpsMonitoring(443, "/api/liveness-probe")
                .WithTags(tags)
                .CreateAsync(cancellationToken);

            _logger.Information("Created Traffic Manager with Id {resourceId}", tm.Id);

            return tm;
        }

        public async Task<ITrafficManagerProfile> GetTrafficManagerAsync(string tmId, CancellationToken cancellationToken = default)
        {
            _logger.Information("Getting a Traffic Manager with Id {resourceId} ...", tmId);
            var tm = await FluentClient
                .TrafficManagerProfiles
                .GetByIdAsync(tmId, cancellationToken);

            return tm;
        }

        public Task<ITrafficManagerProfile> GetTrafficManagerAsync(string rgName, string tmName, CancellationToken cancellationToken = default)
        {
            _logger.Information("Start getting Traffic Manager with name: {tmName} in RG {rgName} ...", tmName, rgName);
            return FluentClient
                .TrafficManagerProfiles
                .GetByResourceGroupAsync(rgName, tmName, cancellationToken);
        }

        public Task<IDnsZone> GetDNSZoneAsync(string rgName, string dnsName, CancellationToken cancellationToken = default)
        {
            _logger.Information("Getting DNS zone with dnsName '{dnsName}' in RG '{resourceGroup}'.", dnsName, rgName);
            return FluentClient
                .DnsZones
                .GetByResourceGroupAsync(rgName, dnsName, cancellationToken);
        }

        public async Task<IDnsZone> CreateDNSZoneAsync(string rgName, string dnsName, IDictionary<string, string> tags, CancellationToken cancellationToken = default)
        {
            var dns = await FluentClient
                .DnsZones
                .Define(dnsName)
                .WithExistingResourceGroup(rgName)
                .WithTags(tags)
                .CreateAsync(cancellationToken);

            _logger.Information("Created DNS zone with id '{resourceId}'.", dns.Id);
            return dns;
        }
        #endregion
    }
}
