using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.DataSource;
using Microsoft.Liftr.Logging.AspNetCore;
using Microsoft.Liftr.TokenManager;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Liftr.Sample.Web
{
    /// <summary>
    /// This is an example class for health check background service.
    /// </summary>
    public class HealthCheckBackgroundService : BackgroundService
    {
        private const string c_countrtName = "pv-index";
        private readonly ICounterEntityDataSource _counter;
        private readonly ISingleTenantAppTokenProvider _sinApp;

        public HealthCheckBackgroundService(
            ICounterEntityDataSource counter,
            ISingleTenantAppTokenProvider sinApp)
        {
            _counter = counter;
            _sinApp = sinApp;

            // We should always return the pre-computed health status to make sure the liveness probe will not timeout.
            HealthCheckExtension.GetHealthCheckStatus = () => IsHealthy;
        }

        public bool IsHealthy { get; set; } = true;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(20));

                try
                {
                    // Check the dependency services to know if they are healthy or not.
                    await _counter.GetCounterAsync(c_countrtName);
                    await _sinApp.GetTokenAsync();

                    IsHealthy = true;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    IsHealthy = false;
                }
            }
        }
    }
}
