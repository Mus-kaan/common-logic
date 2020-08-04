//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public static class NetworkHelper
    {
        public static async Task AddPulicIpToTrafficManagerAsync(IAzure fluentClient, string tmId, string endpointName, string ip, bool enabled, Serilog.ILogger logger)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var tm = await fluentClient.TrafficManagerProfiles.GetByIdAsync(tmId);

            if (tm == null)
            {
                var errMsg = $"Cannot find the traffic manager with Id {tmId}";
                logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            if (string.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip), "ip is invalid. Actual value: " + ip);
            }

            logger.Information("Update endpoints for tm with ResourceId: {ResourceId}", tmId);
            logger.Information("Ip address that want to be added to TM: {PublicIP}", ip);

            foreach (var endpoint in tm.Inner.Endpoints)
            {
                if (endpoint.Target.OrdinalEquals(ip))
                {
                    logger.Information("The same IP is already added to the Traffic Manger.");

                    if (enabled)
                    {
                        await tm.Update()
                        .UpdateExternalTargetEndpoint(endpoint.Name)
                            .WithTrafficEnabled()
                            .Parent()
                        .ApplyAsync();
                    }
                    else
                    {
                        await tm.Update()
                        .UpdateExternalTargetEndpoint(endpoint.Name)
                            .WithTrafficDisabled()
                            .Parent()
                        .ApplyAsync();
                    }

                    return;
                }
            }

            logger.Information("Add the public IP as a new endpoint with name {EndpointName}.", endpointName);
            var update = tm.Update()
                .DefineExternalTargetEndpoint(endpointName)
                .ToFqdn(ip)
                .FromRegion(Region.USWest)
                .WithRoutingWeight(100);

            if (!enabled)
            {
                update = update.WithTrafficDisabled();
            }

            await update.Attach().ApplyAsync();
        }
    }
}
