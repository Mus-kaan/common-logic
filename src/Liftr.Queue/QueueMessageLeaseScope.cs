//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Queues;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Queue
{
    public sealed class QueueMessageLeaseScope : IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public QueueMessageLeaseScope(QueueClient queue, string msgId, string msgPopReceipt, Serilog.ILogger logger)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            if (string.IsNullOrEmpty(msgId))
            {
                throw new ArgumentNullException(nameof(msgId));
            }

            if (string.IsNullOrEmpty(msgPopReceipt))
            {
                throw new ArgumentNullException(nameof(msgPopReceipt));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            PopReceipt = msgPopReceipt;
            SyncMutex = new SemaphoreSlim(1, 1);

            Func<Task> func = async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10.0), _cts.Token);
                        try
                        {
                            await SyncMutex.WaitAsync(_cts.Token);
                            var response = await queue.UpdateMessageAsync(msgId, PopReceipt, null, TimeSpan.FromSeconds(60.0));
                            PopReceipt = response.Value.PopReceipt;
                        }
                        finally
                        {
                            SyncMutex.Release();
                        }
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        if (ex is TaskCanceledException)
                        {
                            return;
                        }

                        logger.Error(ex, "Failed at extending queue message lease.");
                    }
                }
            };

            func();
        }

        public string PopReceipt { get; private set; }

        public SemaphoreSlim SyncMutex { get; }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
