//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Logging.GenericHosting
{
    /// <summary>
    /// Add this to IServiceCollection to make all logs are sent to AppInsights before the console App exit.
    /// </summary>
    internal class AppInsightsFlushService : IHostedService
    {
        private readonly TelemetryClient _client;

        public AppInsightsFlushService(TelemetryClient client)
        {
            _client = client;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Flush();
            return Task.CompletedTask;
        }
    }
}
