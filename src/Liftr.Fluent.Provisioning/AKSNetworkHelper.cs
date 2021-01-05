//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class AKSNetworkHelper
    {
        private readonly ILogger _logger;

        public AKSNetworkHelper(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public Task AddPulicIpToTrafficManagerAsync(IAzure fluentClient, string tmId, string endpointName, string ip, bool enabled)
        {
            return NetworkHelper.AddPulicIpToTrafficManagerAsync(fluentClient, tmId, endpointName, ip, enabled, _logger);
        }

        public async Task<IPublicIPAddress> GetAKSPublicIPAsync(ILiftrAzure liftrAzureClient, string AKSRGName, string AKSName, Region location, IPCategory ipCategory = IPCategory.InOutbound)
        {
            if (liftrAzureClient == null)
            {
                throw new ArgumentNullException(nameof(liftrAzureClient));
            }

            var aksId = $"subscriptions/{liftrAzureClient.FluentClient.SubscriptionId}/resourceGroups/{AKSRGName}/providers/Microsoft.ContainerService/managedClusters/{AKSName}";
            var aks = await liftrAzureClient.GetAksClusterAsync(aksId);
            if (aks == null)
            {
                var ex = new InvalidOperationException($"Cannot find the AKS cluster with resource Id '{aksId}'. We will not be able to find its public IP address. Please make sure the AKS is provisioned first.");
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            var mcRGName = GetMCResourceGroupName(AKSRGName, AKSName, location);
            return await GetAKSPublicIPAsync(liftrAzureClient.FluentClient, mcRGName, ipCategory);
        }

        private async Task<IPublicIPAddress> GetAKSPublicIPAsync(IAzure fluentClient, string mcRGName, IPCategory ipCategory = IPCategory.InOutbound)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Listing all public load balancers in ResourceGroup {mcRGName}...", mcRGName);
            IEnumerable<ILoadBalancer> lbs = null;
            IPublicIPAddress pip = null;

            lbs = (await fluentClient
            .LoadBalancers
            .ListByResourceGroupAsync(mcRGName)).ToList().Where(lb => lb.PublicIPAddressIds.Any());

            _logger.Information("Found {lbCount} public load balancers in ResourceGroup {mcRGName}...", lbs?.Count(), mcRGName);

            if (lbs?.Count() > 1)
            {
                var ex = new InvalidOperationException($"There exists multiple load balancers in the AKS managed '{mcRGName}' resource group. This is not supported, since the egress traffic will be random using one of the front end's IPs.");
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            var lb = lbs?.FirstOrDefault();
            if (lb == null)
            {
                return null;
            }

            var ipAddressCount = lb.PublicIPAddressIds.Count;

            _logger.Information($"Found {ipAddressCount} frontend IPs in the public load balancer {lb.Id} in ResourceGroup {mcRGName}...");

            // When nginx ingress controller deployment is not done, returning null
            if (ipAddressCount == 1)
            {
                return pip;
            }

            /*
            * Earlier design of AKS cluster created using Basic SKU Loadbalancer supports same Basic SKU Public IP to be as Outbound IP on AKS as well Inbound IP on Nginx Ingress Controller Load Balancer
            * Now, we are using Standard SKU Loadbalancer which does not support the earlier design. The Standard SKU Public IP which will be assigned to AKS cluster Load Balancer at creation time, cannot be assigned to Nginx Ingress Controller Load Balancer.
            * If we assign same IP to Nginx Load Balancer and Kubernetes Load Balncer, PublicIPReferencedByMultipleIPConfigs error is thrown.
            * To handle this scenario, we use another static Standard Public IP for Nginx Load Balancer as Inbound(ingress) IP and AKS Kubernetes Load Balancer provisioned IP during AKS creation as Outbound(egress) IP. Reference Link: https://stackoverflow.com/questions/49994073/load-balancer-publicipreferencedbymultipleipconfigs-error-on-restart
            * This ultimately leads to Kubernetes Load Balancer under MC_ rg having 2 front end IPs. Hence removing the check for > 1 IP count.
            */

            if (ipAddressCount == 2)
            {
                var pip1 = await fluentClient.PublicIPAddresses.GetByIdAsync(lb.PublicIPAddressIds[0]);
                var pip2 = await fluentClient.PublicIPAddresses.GetByIdAsync(lb.PublicIPAddressIds[1]);
                pip = GetAKSPublicIPAddress(pip1, pip2, ipCategory);
            }

            if (ipAddressCount != 1 && ipAddressCount != 2)
            {
                var ex = new InvalidOperationException($"Found {ipAddressCount} frontend IPs in the public load balancer {lb.Id} in ResourceGroup {mcRGName}...");
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            _logger.Information($"Found IP Address {pip.IPAddress} from AKS network with id {pip.Id}");

            return pip;
        }

        private static IPublicIPAddress GetAKSPublicIPAddress(IPublicIPAddress pip1, IPublicIPAddress pip2, IPCategory ipCategory = IPCategory.InOutbound)
        {
            var pip1Name = pip1.Name;
            var pip2Name = pip2.Name;
            IPublicIPAddress pip = null;

            if (pip1Name.OrdinalContains(ipCategory.ToString()))
            {
                pip = pip1;
            }

            if (pip2Name.OrdinalContains(ipCategory.ToString()))
            {
                pip = pip2;
            }

            return pip;
        }

        private static string GetMCResourceGroupName(string AKSRGName, string AKSName, Region location)
            => $"MC_{AKSRGName}_{AKSName}_{location}";
    }
}
