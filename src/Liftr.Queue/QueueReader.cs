//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Queues;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Logging;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Microsoft.Liftr.Queue.Tests")]

namespace Microsoft.Liftr.Queue
{
    public sealed class QueueReader : IQueueReader
    {
        private readonly QueueClient _queue;
        private readonly QueueReaderOptions _options;
        private readonly ITimeSource _timeSource;
        private readonly Serilog.ILogger _logger;
        private readonly object _syncObj = new object();
        private static readonly TimeSpan s_maxWaitTime = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan s_minWaitTime = TimeSpan.FromMilliseconds(71);
        private bool _started;
        private TimeSpan _waitTime = s_minWaitTime;

        public QueueReader(
            QueueClient queue,
            QueueReaderOptions options,
            ITimeSource timeSource,
            Serilog.ILogger logger)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            options.CheckValues();
        }

        internal Exception ReaderException { get; private set; }

        public async Task StartListeningAsync(Func<LiftrQueueMessage, QueueMessageProcessingResult, CancellationToken, Task> messageProcessingCallback, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_started)
            {
                var ex = new InvalidOperationException($"{nameof(StartListeningAsync)} can only be called once.");
            }

            _started = true;

            Func<Task> func = async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TimeSpan waitTime = _waitTime;
                    try
                    {
                        var messages = (await _queue.ReceiveMessagesAsync(maxMessages: 1, visibilityTimeout: TimeSpan.FromSeconds(40), cancellationToken: cancellationToken)).Value;
                        var queueMessage = messages?.FirstOrDefault();

                        if (queueMessage != null)
                        {
                            waitTime = GetWaitTime(true);
                            var message = queueMessage.MessageText.FromJson<LiftrQueueMessage>();
                            message.InsertedOn = queueMessage.InsertedOn;
                            message.ExpiresOn = queueMessage.ExpiresOn;
                            message.DequeueCount = queueMessage.DequeueCount;

                            LogEventLevel? overrideLevel = null;
                            if (message.MsgTelemetryContext != null)
                            {
                                CallContextHolder.ClientRequestId.Value = message.MsgTelemetryContext.ClientRequestId;
                                CallContextHolder.ARMRequestTrackingId.Value = message.MsgTelemetryContext.ARMRequestTrackingId;
                                CallContextHolder.CorrelationId.Value = message.MsgTelemetryContext.CorrelationId;

                                if (Enum.TryParse<LogEventLevel>(message.MsgTelemetryContext.LogFilterOverwrite, true, out var level))
                                {
                                    overrideLevel = level;

                                    // Pass the log level filter to the next tier.
                                    CallContextHolder.LogFilterOverwrite.Value = message.MsgTelemetryContext.LogFilterOverwrite;
                                }
                            }

                            try
                            {
                                using (var lease = new QueueMessageLeaseScope(_queue, queueMessage.MessageId, queueMessage.PopReceipt, _logger))
                                using (var logFilterOverrideScope = new LogFilterOverrideScope(overrideLevel))
                                using (new LogContextPropertyScope("LiftrTrackingId", message.MsgTelemetryContext?.ARMRequestTrackingId))
                                using (new LogContextPropertyScope("LiftrCorrelationId", message.MsgTelemetryContext?.CorrelationId))
                                using (new LogContextPropertyScope("LiftrQueueMessageId", message.MsgId))
                                using (var operation = _logger.StartTimedOperation("ProcessQueueMessage"))
                                {
                                    var processingResult = new QueueMessageProcessingResult();
                                    try
                                    {
                                        await messageProcessingCallback(message, processingResult, cancellationToken);
                                    }
#pragma warning disable CA1031 // Do not catch general exception types
                                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                                    {
                                        _logger.Error(ex, $"{nameof(messageProcessingCallback)} threw unhandled exception.");
                                        processingResult.SuccessfullyProcessed = false;
                                        processingResult.ProcessingError = ex.Message;
                                    }
                                    finally
                                    {
                                        if (processingResult.SuccessfullyProcessed)
                                        {
                                            var endTme = _timeSource.UtcNow;
                                            var duration = endTme - message.CreatedAt.ParseZuluDateTime();
                                            _logger.Information("Finished processing queue message. MsgId: {MsgId}, DurationInSeconds: {DurationInSeconds}, CreatedAt:{CreatedAt}, FinishedAt: {FinishedAt}", message.MsgId, duration.TotalSeconds, message.CreatedAt, endTme.ToZuluString());
                                            try
                                            {
                                                await lease.SyncMutex.WaitAsync(cancellationToken);
                                                await _queue.DeleteMessageAsync(queueMessage.MessageId, lease.PopReceipt);
                                            }
                                            finally
                                            {
                                                lease.SyncMutex.Release();
                                            }
                                        }
                                        else
                                        {
                                            operation.FailOperation(processingResult.ProcessingError);
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                CallContextHolder.LogFilterOverwrite.Value = null;
                                CallContextHolder.ClientRequestId.Value = null;
                                CallContextHolder.ARMRequestTrackingId.Value = null;
                                CallContextHolder.CorrelationId.Value = null;
                            }
                        }
                        else
                        {
                            waitTime = GetWaitTime(false);
                        }
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        _logger.Error(ex, "Issue happend at processing essage from queue.");
                        ReaderException = ex;
                    }

                    await Task.Delay(waitTime, cancellationToken);
                }
            };

            List<Task> workers = new List<Task>();
            for (int i = 0; i < _options.MaxConcurrentCalls; i++)
            {
                workers.Add(func());
            }

            await Task.WhenAll(workers);
        }

        private TimeSpan GetWaitTime(bool processedMessage)
        {
            lock (_syncObj)
            {
                if (processedMessage)
                {
                    _waitTime = s_minWaitTime;
                }
                else
                {
                    // Exponential backoff.
                    _waitTime = TimeSpan.FromMilliseconds(1.7 * _waitTime.TotalMilliseconds);
                    if (_waitTime > s_maxWaitTime)
                    {
                        _waitTime = s_maxWaitTime;
                    }
                }
            }

            return _waitTime;
        }
    }
}
