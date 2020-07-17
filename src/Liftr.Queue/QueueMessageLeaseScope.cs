//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Queue
{
    public sealed class QueueMessageLeaseScope : IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public QueueMessageLeaseScope(QueueClient queue, QueueMessage msg, Serilog.ILogger logger)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            if (msg == null)
            {
                throw new ArgumentNullException(nameof(msg));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            PopReceipt = msg.PopReceipt;
            SyncMutex = new SemaphoreSlim(1, 1);

            Func<Task> func = async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(QueueParameters.MessageLeaseRenewInterval, _cts.Token);
                        try
                        {
                            await SyncMutex.WaitAsync(_cts.Token);
                            var response = await queue.UpdateMessageAsync(msg.MessageId, PopReceipt, msg.MessageText, visibilityTimeout: QueueParameters.VisibilityTimeout);
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
