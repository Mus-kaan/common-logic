//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Storage.Queue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Queue
{
    public sealed class QueueMessageLeaseScope : IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public QueueMessageLeaseScope(CloudQueue queue, CloudQueueMessage msg, Serilog.ILogger logger)
        {
            Func<Task> func = async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        await queue.UpdateMessageAsync(msg, TimeSpan.FromSeconds(30.0), MessageUpdateFields.Visibility, _cts.Token);
                        await Task.Delay(TimeSpan.FromSeconds(10.0), _cts.Token);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        logger.Error(ex, "Failed at extending queue message lease.");
                    }
                }
            };
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
