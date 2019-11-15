//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Queues;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DiagnosticSource;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Queue
{
    public sealed class QueueWriter : IQueueWriter
    {
        private readonly QueueClient _queue;
        private readonly ITimeSource _timeSource;
        private readonly Serilog.ILogger _logger;
        private readonly string _msgIdPrefix;
        private int _msgCount = 0;

        public QueueWriter(QueueClient queue, ITimeSource timeSource, Serilog.ILogger logger)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _msgIdPrefix = $"{Environment.MachineName}-{timeSource.UtcNow.ToDateString()}-";
        }

        public async Task AddMessageAsync(string message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            var i = Interlocked.Increment(ref _msgCount);
            var msg = new LiftrQueueMessage()
            {
                MsgId = $"{_msgIdPrefix}{i:D8}",
                Content = message,
                MsgTelemetryContext = TelemetryContext.GetCurrent(),
                CreatedAt = _timeSource.UtcNow.ToZuluString(),
            };

            await _queue.SendMessageAsync(msg.ToJson(), timeToLive: TimeSpan.FromMinutes(60));
            _logger.Debug("Added message with Id 'MsgId' in queue.", msg.MsgId);
        }
    }
}
