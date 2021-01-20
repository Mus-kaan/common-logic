//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
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

            await tm.WithExternalIpAsync(endpointName, ip, enabled, logger);
        }
    }
}
