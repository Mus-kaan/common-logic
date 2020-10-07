//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Queue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ClassicQueue
{
    public sealed class ClassicQueueWriter : IQueueWriter
    {
        private readonly CloudQueue _queue;
        private readonly ITimeSource _timeSource;
        private readonly Serilog.ILogger _logger;
        private readonly string _msgIdPrefix;
        private readonly TimeSpan _messageTimeToLive;
        private readonly TimeSpan? _messageVisibilityTimeout;
        private int _msgCount = 0;

        /// <summary>
        /// Azure message queue writer augmented with Liftr trace context. For more information see https://docs.microsoft.com/en-us/rest/api/storageservices/put-message.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="timeSource"></param>
        /// <param name="logger"></param>
        /// <param name="messageVisibilityTimeout">Message visibility timeout, which will make the message invisible until the visibility timeout expires.
        /// Optional with a default value of 0. Cannot be larger than 7 days.</param>
        /// <param name="messageTimeToLive">Specifies the time-to-live interval for the message. Default to 60 minutes.</param>
        public ClassicQueueWriter(
            CloudQueue queue,
            ITimeSource timeSource,
            Serilog.ILogger logger,
            TimeSpan? messageVisibilityTimeout = null,
            TimeSpan? messageTimeToLive = null)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _msgIdPrefix = $"{Environment.MachineName}-";
            _messageTimeToLive = messageTimeToLive.HasValue ? messageTimeToLive.Value : TimeSpan.FromMinutes(60);
            if (messageVisibilityTimeout.HasValue && messageVisibilityTimeout.Value > TimeSpan.FromDays(7))
            {
                throw new ArgumentOutOfRangeException(nameof(messageVisibilityTimeout), "Cannot be larger than 7 days");
            }

            _messageVisibilityTimeout = messageVisibilityTimeout;
        }

        public async Task AddMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            var i = Interlocked.Increment(ref _msgCount);
            var msg = new LiftrQueueMessage()
            {
                MsgId = $"{_msgIdPrefix}{_timeSource.UtcNow.ToDateString()}-{i:D8}",
                Content = message,
                MsgTelemetryContext = TelemetryContext.GetCurrent(),
                CreatedAt = _timeSource.UtcNow.ToZuluString(),
            };

            CloudQueueMessage qMessage = new CloudQueueMessage(msg.ToJson());
            await _queue.AddMessageAsync(qMessage, _messageTimeToLive, _messageVisibilityTimeout, new QueueRequestOptions(), new OperationContext(), cancellationToken);

            _logger.Information("Added message with Id '{MsgId}' into queue. TTL: {msgTTL}, visibilityTimeout: {visibilityTimeout}", msg.MsgId, _messageTimeToLive, _messageVisibilityTimeout);
        }
    }
}
