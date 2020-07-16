//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.DataSource;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Queue;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Sample.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ICounterEntityDataSource _counter;
        private readonly IQueueReader _qReader;
        private readonly Serilog.ILogger _logger;
        private int _cnt = 0;

        public Worker(ICounterEntityDataSource counter, IQueueReader qReader, Serilog.ILogger logger)
        {
            _counter = counter;
            _qReader = qReader;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Worker is starting.");
            _logger.LogProcessStart();

            _ = _qReader.StartListeningAsync(ProcessQueueMessageAsync, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Information("Worker running at: {time}", DateTimeOffset.Now);
                await DoWorkAsync();
                await Task.Delay(10000, stoppingToken);
            }
        }

        private async Task DoWorkAsync()
        {
            _cnt++;
            _logger.Information("Timed Background Service is working.");
            await InstanceMetaHelper.GetMetaInfoAsync();
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

        private async Task ProcessQueueMessageAsync(LiftrQueueMessage message, QueueMessageProcessingResult qResult, CancellationToken token)
        {
            using (var op = _logger.StartTimedOperation("WorkerProcessQueueMessage"))
            {
                _logger.Information("Received queue message content: {msgContent}", message.Content);
                var pv = await _counter.GetCounterAsync("pv-index") ?? 0;
                _logger.Information("Current page view count: {pvCnt}", pv);

                try
                {
                    _logger.Information("Before start http get.");
                    await Task.Delay(300);
                    if (_cnt % 5 == 2)
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
                    qResult.SuccessfullyProcessed = false;
                }
            }
        }
    }
}
