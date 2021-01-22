//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Liftr.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr
{
    public static class TrafficManagerExtensions
    {
        public static async Task WithExternalIpAsync(this ITrafficManagerProfile tm, string endpointName, string ip, bool enabled, Serilog.ILogger logger)
        {
            if (tm == null)
            {
                throw new ArgumentNullException(nameof(tm));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (string.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip), "ip is invalid. Actual value: " + ip);
            }

            logger.Information("Update endpoints for tm with ResourceId: {ResourceId}", tm.Id);
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

        public static async Task WithTrafficManagerEndpointAsync(this ITrafficManagerProfile tm, ILiftrAzure liftrAzure, ITrafficManagerProfile targetTM, Region region, Serilog.ILogger logger)
        {
            if (tm == null)
            {
                throw new ArgumentNullException(nameof(tm));
            }

            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            if (targetTM == null)
            {
                throw new ArgumentNullException(nameof(targetTM));
            }

            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Information("Update endpoints for tm with ResourceId '{ResourceId}'", tm.Id);
            logger.Information("Targeting TM to add as endpoint: {targetTM}", targetTM.Id);
            var endpointName = targetTM.Name;

            List<string> endpointsToRemove = new List<string>();

            foreach (var endpoint in tm.Inner.Endpoints)
            {
                if (endpoint.TargetResourceId?.OrdinalEquals(targetTM.Id) == true)
                {
                    logger.Information("The same TM endpoint is already added to the Traffic Manger.");
                    return;
                }
                else if (endpoint.TargetResourceId?.OrdinalContains("trafficManagerProfiles") == true)
                {
                    var epTM = await liftrAzure.GetTrafficManagerAsync(endpoint.TargetResourceId);
                    if (epTM == null)
                    {
                        endpointsToRemove.Add(endpoint.Name);
                    }
                }
            }

            if (endpointsToRemove.Any())
            {
                logger.Information("Remove invalid TM enpoints: {@endpointsToRemove}", endpointsToRemove);
                var update = tm.Update();

                foreach (var ep in endpointsToRemove)
                {
                    update = update.WithoutEndpoint(ep);
                }

                tm = await update.ApplyAsync();
            }

            logger.Information("Add the TM as a new endpoint with name {EndpointName}.", endpointName);

            await tm.Update()
                .DefineNestedTargetEndpoint(endpointName)
                .ToProfile(targetTM)
                .FromRegion(region)
                .WithMinimumEndpointsToEnableTraffic(1)
                .Attach()
                .ApplyAsync();
        }
    }
}
