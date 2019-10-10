//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning.MultiTenant
{
    public class ComputeV1
    {
        private readonly ILiftrAzure _azure;
        private readonly ILogger _logger;

        public ComputeV1(ILiftrAzure azureClient, ILogger logger)
        {
            _azure = azureClient;
            _logger = logger;
        }

        public async Task<IResourceGroup> CreateServiceClusterAsync(string baseName, NamingContext context, VirtualMachineScaleSetSkuTypes type, string vmUsername, string vmPassword, int vmCount = 1)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _logger.Information("Creating Resource Group ...");
            var rg = await _azure.CreateResourceGroupAsync(context.Location, context.ResourceGroupName(baseName), context.Tags);
            _logger.Information($"Created Resource Group with Id {rg.Id}");

            var leafDomainName = context.LeafDomainName(baseName);
            var publicIpAddress = await _azure.FluentClient
                .PublicIPAddresses
                .Define("public-lb-pip")
                .WithRegion(context.Location)
                .WithExistingResourceGroup(rg.Name)
                .WithSku(PublicIPSkuType.Standard)
                .WithTags(context.Tags)
                .WithStaticIP()
                .WithLeafDomainLabel(leafDomainName)
                .CreateAsync();
            _logger.Information("Created public ip address with Id {@resourceId}. Fqdn: {@Fqdn}", publicIpAddress.Id, publicIpAddress.Fqdn);

            var backendSubnetName = "backend";
            var privateLinkFrontendSubnetName = "private-link-frontend";
            var internetFrontendSubnetName = "internet-frontend";

            _logger.Information("Start deploying VNet for Private link Service ...");
            var vnetName = NamingContext.VNetName(baseName);
            var vnetTemplate = TemplateHelper.GeneratePLSVNetTemplate(
                context.Location,
                context.Tags,
                vnetName,
                vnetCIDR: "10.248.0.0/13",
                plsSubnet: privateLinkFrontendSubnetName,
                plsSubnetCIDR: "10.255.0.0/16",
                publicFrontSubnet: internetFrontendSubnetName,
                publicFrontSubnetCIDR: "10.254.0.0/16",
                backendSubnet: backendSubnetName,
                backendSubnetCIDR: "10.252.0.0/16");
            var ventDeployment = await _azure.CreateDeploymentAsync(context.Location, rg.Name, vnetTemplate);
            var vnet = await _azure.FluentClient
                .Networks
                .GetByResourceGroupAsync(rg.Name, vnetName);
            _logger.Information("Created VNet with Id {@resourceId}", vnet.Id);

            ILoadBalancer privateLinkLB = null, internetLB = null;
            var ilbFrontendName = "ilbPrivateLinkFrontend";
            var ilbBackendPoolName = "ilbBackendPool";
            var plbBackendPoolName = "publiLBBackendPool";
            var rdpNATPool = "rdp-nat-pool";
            {
                var lbName = NamingContext.InternalLoadBalancerName("private-link");
                _logger.Information("Start creating an Internal Load Balancer with name {@loadBalancerName} for private endpoint to connect ...", lbName);
                var ilbProbName = lbName + "tcp3389";
                privateLinkLB = await _azure.FluentClient
                   .LoadBalancers
                   .Define(lbName)
                   .WithRegion(context.Location)
                   .WithExistingResourceGroup(rg.Name)
                   .DefineLoadBalancingRule(lbName + "-tcp80")
                       .WithProtocol(TransportProtocol.Tcp)
                       .FromFrontend(ilbFrontendName)
                       .FromFrontendPort(80)
                       .ToBackend(ilbBackendPoolName)
                       .ToBackendPort(80)
                       .WithProbe(ilbProbName)
                       .WithIdleTimeoutInMinutes(15)
                       .Attach()
                    .DefineLoadBalancingRule(lbName + "-tcp443")
                       .WithProtocol(TransportProtocol.Tcp)
                       .FromFrontend(ilbFrontendName)
                       .FromFrontendPort(443)
                       .ToBackend(ilbBackendPoolName)
                       .ToBackendPort(443)
                       .WithProbe(ilbProbName)
                       .WithIdleTimeoutInMinutes(15)
                       .Attach()
                    .WithSku(LoadBalancerSkuType.Standard)
                    .WithTags(context.Tags)
                    .DefinePrivateFrontend(ilbFrontendName)
                        .WithExistingSubnet(vnet, privateLinkFrontendSubnetName)
                        .WithPrivateIPAddressStatic("10.255.255.101")
                        .Attach()
                    .DefineTcpProbe(ilbProbName)
                        .WithPort(3389)
                        .WithIntervalInSeconds(10)
                        .WithNumberOfProbes(2)
                        .Attach()
                    .CreateAsync();
                _logger.Information("Created Internal Load Balancer with Id {@resourceId}", privateLinkLB.Id);
            }

            {
                var lbName = NamingContext.PublicLoadBalancerName("internet");
                _logger.Information("Start creating an Internet Load Balancer with name {@loadBalancerName} for public access ...", lbName);
                var frontendName = "internetFrontend";
                var probName = lbName + "tcp3389";
                internetLB = await _azure.FluentClient
                   .LoadBalancers
                   .Define(lbName)
                   .WithRegion(context.Location)
                   .WithExistingResourceGroup(rg.Name)
                   .DefineLoadBalancingRule(lbName + "-tcp80")
                       .WithProtocol(TransportProtocol.Tcp)
                       .FromFrontend(frontendName)
                       .FromFrontendPort(80)
                       .ToBackend(plbBackendPoolName)
                       .WithProbe(probName)
                       .WithIdleTimeoutInMinutes(15)
                       .Attach()
                    .DefineLoadBalancingRule(lbName + "-tcp443")
                       .WithProtocol(TransportProtocol.Tcp)
                       .FromFrontend(frontendName)
                       .FromFrontendPort(443)
                       .ToBackend(plbBackendPoolName)
                       .WithProbe(probName)
                       .WithIdleTimeoutInMinutes(15)
                       .Attach()
                    .DefineInboundNatPool(rdpNATPool)
                        .WithProtocol(TransportProtocol.Tcp)
                        .FromFrontend(frontendName)
                        .FromFrontendPortRange(7000, 7200)
                        .ToBackendPort(3389)
                        .Attach()
                    .WithSku(LoadBalancerSkuType.Standard)
                    .WithTags(context.Tags)
                    .DefinePublicFrontend(frontendName)
                        .WithExistingPublicIPAddress(publicIpAddress)
                        .Attach()
                    .DefineTcpProbe(probName)
                        .WithPort(3389)
                        .WithIntervalInSeconds(10)
                        .WithNumberOfProbes(2)
                        .Attach()
                    .CreateAsync();
                _logger.Information("Created internet Load Balancer with Id {@resourceId}", internetLB.Id);
            }

            var nsg = await _azure.FluentClient
                .NetworkSecurityGroups
                .Define(NamingContext.NSGName("private", "link"))
                .WithRegion(context.Location)
                .WithExistingResourceGroup(rg.Name)
                .WithTags(context.Tags)
                .AllowAny80InBound()
                .AllowAny443InBound()
                .DefineRule("AllowRDPRange")
                    .AllowInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAnyAddress()
                    .ToPortRange(7000, 7200)
                    .WithAnyProtocol()
                    .WithPriority(3800)
                    .Attach()
                .CreateAsync();
            _logger.Information("Created NSG with Id {@resourceId}", nsg.Id);

            var vmss = await _azure.FluentClient
                .VirtualMachineScaleSets
                .Define("vmss")
                .WithRegion(context.Location)
                .WithExistingResourceGroup(rg.Name)
                .WithSku(type)
                .WithExistingPrimaryNetworkSubnet(vnet, backendSubnetName)
                .WithExistingPrimaryInternetFacingLoadBalancer(internetLB)
                .WithPrimaryInternetFacingLoadBalancerBackends(plbBackendPoolName)
                .WithPrimaryInternetFacingLoadBalancerInboundNatPools(rdpNATPool)
                .WithExistingPrimaryInternalLoadBalancer(privateLinkLB)
                .WithPrimaryInternalLoadBalancerBackends(ilbBackendPoolName)
                .WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer", "2016-Datacenter") // .WithPopularWindowsImage(KnownWindowsVirtualMachineImage.WindowsServer2012R2Datacenter)
                .WithAdminUsername(vmUsername)
                .WithAdminPassword(vmPassword)
                .WithCapacity(vmCount)
                .WithBootDiagnostics()
                .WithExistingNetworkSecurityGroup(nsg)
                .WithTags(context.Tags)
                .CreateAsync();
            _logger.Information("Created VM Scale Set with Id {@resourceId}", vmss.Id);

            _logger.Information("Start deploying Private link Service ...");
            var ilbPrivateLinkSubnet = vnet.Subnets[privateLinkFrontendSubnetName];
            var frontendId = $"{privateLinkLB.Id}/frontendIpConfigurations/{ilbFrontendName}";
            var plsTemplate = TemplateHelper.GeneratePrivateLinkServiceTemplate(context.Location, context.ShortPartnerName + "-private-network", ilbPrivateLinkSubnet.Inner.Id, frontendId);
            var plsDeployment = await _azure.CreateDeploymentAsync(context.Location, rg.Name, plsTemplate);
            _logger.Information("Finished private link service deployment with Id {@IDeployment}", plsDeployment);

            return rg;
        }
    }
}
