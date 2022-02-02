//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal static class IPv6SubnetHelper
    {
        public static async Task CreateIPv6SubnetAsync(
            ILiftrAzure liftrAzure,
            INetwork vnet,
            string subnetName,
            INetworkSecurityGroup nsg,
            string ipv4AddressPrefix,
            string ipv6AddressPrefix,
            CancellationToken cancellationToken)
        {
            if (vnet == null)
            {
                throw new ArgumentNullException(nameof(vnet));
            }

            if (nsg == null)
            {
                throw new ArgumentNullException(nameof(nsg));
            }

            using var nrpClient = new Azure.Management.Network.NetworkManagementClient(liftrAzure.AzureCredentials);
            nrpClient.SubscriptionId = liftrAzure.DefaultSubscriptionId;

            using var ops = liftrAzure.Logger.StartTimedOperation("CreateIPv6Subnet");
            try
            {
                var retrievedNsg = await nrpClient.NetworkSecurityGroups.GetAsync(nsg.ResourceGroupName, nsg.Name);

                var subnetParameters = new Microsoft.Azure.Management.Network.Models.Subnet()
                {
                    AddressPrefixes = new List<string> { ipv4AddressPrefix, ipv6AddressPrefix },
                    NetworkSecurityGroup = retrievedNsg,
                    ServiceEndpoints = new List<ServiceEndpointPropertiesFormat>
                    {
                        new ServiceEndpointPropertiesFormat("Microsoft.AzureCosmosDB"),
                        new ServiceEndpointPropertiesFormat("Microsoft.KeyVault"),
                        new ServiceEndpointPropertiesFormat("Microsoft.Storage"),
                    },
                };

                var created = await nrpClient.Subnets.CreateOrUpdateAsync(vnet.ResourceGroupName, vnet.Name, subnetName, subnetParameters, cancellationToken);
            }
            catch (Exception ex)
            {
                liftrAzure.Logger.Error(ex, "Create subnet failed.");
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public static async Task<Subnet> GetSubnetAsync(
            ILiftrAzure liftrAzure,
            INetwork vnet,
            string subnetName,
            CancellationToken cancellationToken)
        {
            if (vnet == null)
            {
                throw new ArgumentNullException(nameof(vnet));
            }

            if (string.IsNullOrEmpty(subnetName))
            {
                throw new ArgumentNullException(nameof(subnetName));
            }

            using var nrpClient = new Azure.Management.Network.NetworkManagementClient(liftrAzure.AzureCredentials);
            nrpClient.SubscriptionId = liftrAzure.DefaultSubscriptionId;

            try
            {
                return await nrpClient.Subnets.GetAsync(vnet.ResourceGroupName, vnet.Name, subnetName, cancellationToken: cancellationToken);
            }
            catch (Rest.Azure.CloudException ex) when (ex?.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
