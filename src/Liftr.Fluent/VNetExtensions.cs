//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr
{
    public static class VNetExtensions
    {
        public static async Task<INetwork> RemoveEmptySubnetsAsync(this INetwork vnet, Serilog.ILogger logger)
        {
            if (vnet == null)
            {
                throw new ArgumentNullException(nameof(vnet));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var emptySubnets = vnet
                .Subnets
                .Where(s => !s.Key.OrdinalContains("default"))
                .Where(s => s.Value.NetworkInterfaceIPConfigurationCount == 0);

            if (!emptySubnets.Any())
            {
                return vnet;
            }

            var update = vnet.Update();
            foreach (var subnet in emptySubnets)
            {
                logger.Information("Removing empty subnet with name '{subnet}' in vnet '{vnetId}'", subnet.Key, vnet.Id);
                update = update.WithoutSubnet(subnet.Key);
            }

            return await update.ApplyAsync();
        }
    }
}
