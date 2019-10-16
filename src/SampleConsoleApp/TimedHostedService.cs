﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Logging;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GenericHostSample
{
    #region snippet1
    internal class TimedHostedService : IHostedService, IDisposable
    {
        private readonly Serilog.ILogger _logger;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private int _cnt = 0;

        public TimedHostedService(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Timed Background Service is starting.");

            _ = StartPeriodicWorkAsync();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Timed Background Service is stopping.");

            _cts.Cancel();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts.Dispose();
        }

        private async Task StartPeriodicWorkAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(2000, _cts.Token);
                await DoWorkAsync();
            }
        }

        private async Task DoWorkAsync()
        {
            _cnt++;
            _logger.Information("Timed Background Service is working.");
            using (var op = _logger.StartTimedOperation("GetMSWebPage"))
            {
                op.SetContextProperty("CntVal", _cnt);
                try
                {
                    _logger.Information("Before start http get.");
                    await Task.Delay(300);
                    if (_cnt % 3 == 2)
                    {
                        throw new InvalidOperationException($"num: {_cnt}");
                    }

                    using (var client = new HttpClient())
                    {
#pragma warning disable CA2234 // Pass system uri objects instead of strings
                        var result = await client.GetStringAsync("https://microsoft.com");
#pragma warning restore CA2234 // Pass system uri objects instead of strings
                        _logger.Debug("Respose length: {length}", result.Length);
                        op.SetProperty("ResponseLength", result.Length);
                        op.SetResultDescription("Get ms web succeed.");
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _logger.Error(ex, "Do work failed.");
                    op.FailOperation("Do work failed.");
                }
            }
        }
    }
    #endregion
}