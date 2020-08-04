//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Serilog;
using System;
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

        public async Task<IPublicIPAddress> GetAKSPublicIPAsync(ILiftrAzure liftrAzureClient, string AKSRGName, string AKSName, Region location)
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
            return await GetAKSPublicIPAsync(liftrAzureClient.FluentClient, mcRGName);
        }

        private async Task<IPublicIPAddress> GetAKSPublicIPAsync(IAzure fluentClient, string mcRGName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Listing all public load balancers in ResourceGroup {mcRGName}...", mcRGName);
            var lbs = (await fluentClient
                .LoadBalancers
                .ListByResourceGroupAsync(mcRGName)).ToList().Where(lb => lb.PublicIPAddressIds.Any());
            _logger.Information("Found {lbCount} public load balancers in ResourceGroup {mcRGName}...", lbs.Count(), mcRGName);

            if (lbs.Count() > 1)
            {
                var ex = new InvalidOperationException($"There exists multiple load balancers in the AKS managed '{mcRGName}' resource group. This is not supported, since the egress traffic will be random using one of the front end's IPs.");
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            var lb = lbs.FirstOrDefault();
            if (lb == null)
            {
                return null;
            }

            if (lb.PublicIPAddressIds.Count > 1)
            {
                var ex = new InvalidOperationException($"There exists multiple frontend IPs in the public load balancer '{lb.Id}'. This is not supported, since the egress traffic will be random using one of the front end's IPs.");
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            return await fluentClient.PublicIPAddresses.GetByIdAsync(lb.PublicIPAddressIds[0]);
        }

        private static string GetMCResourceGroupName(string AKSRGName, string AKSName, Region location)
            => $"MC_{AKSRGName}_{AKSName}_{location}";
    }
}
