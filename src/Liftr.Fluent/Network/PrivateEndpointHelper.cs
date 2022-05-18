//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Network
{
    internal static class PrivateEndpointHelper
    {
        public static async Task<PrivateEndpoint> CreatePrivateEndpointAsync(ILiftrAzure liftrAzure, string name, string rg, string subnet, string location, string privateLinkServiceId, CancellationToken cancellationToken)
        {
            using var nrpClient = new NetworkManagementClient(liftrAzure.AzureCredentials);
            nrpClient.SubscriptionId = liftrAzure.DefaultSubscriptionId;

            PrivateLinkServiceConnection connection = new PrivateLinkServiceConnection()
            {
                PrivateLinkServiceId = privateLinkServiceId,
                Name = name,
            };

            var privateEndpoint = new PrivateEndpoint()
            {
                Subnet = new Subnet()
                {
                    Id = subnet,
                },
                ManualPrivateLinkServiceConnections = new List<PrivateLinkServiceConnection>() { connection },
                Location = location,
            };
            var privateEndpointCreate = await nrpClient.PrivateEndpoints.CreateOrUpdateAsync(rg, name, privateEndpoint, cancellationToken);
            return privateEndpointCreate;
        }

        public static async Task<string> GetIPAddressAsync(ILiftrAzure liftrAzure, string rg, PrivateEndpoint privateEndpoint)
        {
            using var nrpClient = new NetworkManagementClient(liftrAzure.AzureCredentials);
            nrpClient.SubscriptionId = liftrAzure.DefaultSubscriptionId;

            string regex = "networkInterfaces/";
            string nwInterface = privateEndpoint.NetworkInterfaces[0].Id.Split(regex.ToCharArray())[1];
            NetworkInterface nic = await nrpClient.NetworkInterfaces.GetAsync(rg, nwInterface);
            string ip = nic.IpConfigurations[0].PrivateIPAddress;

            return ip;
        }
    }
}
