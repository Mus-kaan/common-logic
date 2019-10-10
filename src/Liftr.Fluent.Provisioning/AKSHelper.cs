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
    public class AKSHelper
    {
        private readonly ILogger _logger;

        public AKSHelper(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public async Task AddPulicIpToTrafficManagerAsync(IAzure fluentClient, string tmId, string endpointName, string ip, bool enabled)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            var tm = await fluentClient.TrafficManagerProfiles.GetByIdAsync(tmId);

            if (tm == null)
            {
                var errMsg = $"Cannot find the traffic manager with Id {tmId}";
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            if (string.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip), "ip is invalid. Actual value: " + ip);
            }

            _logger.Information("Update endpoints for tm with ResourceId: {ResourceId}", tmId);
            _logger.Information("Ip address that want to be added to TM: {PublicIP}", ip);

            foreach (var endpoint in tm.Inner.Endpoints)
            {
                if (endpoint.Target.OrdinalEquals(ip))
                {
                    _logger.Information("The same IP is already added to the Traffic Manger.");

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

            _logger.Information("Add the public IP as a new endpoint with name {EndpointName}.", endpointName);
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

        public Task<IPublicIPAddress> GetAppPublicIpAsync(IAzure fluentClient, string AKSRGName, string AKSName, Region location, string aksSvcLabel)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            var mcRGName = GetMCResourceGroupName(AKSRGName, AKSName, location);
            return GetAppPublicIpAsync(fluentClient, mcRGName, aksSvcLabel);
        }

        private async Task<IPublicIPAddress> GetAppPublicIpAsync(IAzure fluentClient, string mcRGName, string aksSvcLabel)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Listing all public IP addresses in ResourceGroup {mcRGName}...", mcRGName);
            var ips = await fluentClient.PublicIPAddresses.ListByResourceGroupAsync(mcRGName);
            _logger.Information("Found {ipCount} public IP addresses in ResourceGroup {mcRGName}...", ips.Count(), mcRGName);
            foreach (var ip in ips)
            {
                if (ip.Tags.ContainsKey("service"))
                {
                    if (ip.Tags["service"].Contains(aksSvcLabel))
                    {
                        return ip;
                    }
                }
            }

            return null;
        }

        private static string GetMCResourceGroupName(string AKSRGName, string AKSName, Region location)
            => $"MC_{AKSRGName}_{AKSName}_{location}";
    }
}
