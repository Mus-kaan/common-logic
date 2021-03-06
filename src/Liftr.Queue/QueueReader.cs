//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
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
        private readonly object _waitTimeLock = new object();
        private bool _started;
        private TimeSpan _waitTime = QueueParameters.ScanMinWaitTime;
        private TimeSpan _visibilityTime = QueueParameters.VisibilityTimeout;
        private TimeSpan _messageLeaseRenewTime = QueueParameters.MessageLeaseRenewInterval;

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
            _visibilityTime = TimeSpan.FromSeconds(options.VisibilityTimeoutInSeconds);
            _messageLeaseRenewTime = TimeSpan.FromSeconds(options.MessageLeaseRenewIntervalInSeconds);
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
                        var queueMessage = await GetQueueMessageAsync(cancellationToken);

                        if (queueMessage != null)
                        {
                            var srpMsgId = queueMessage.MessageId;
                            using var srpMsgIdScope = new LogContextPropertyScope(nameof(srpMsgId), srpMsgId);

                            // This is for handling the message with probably invalid format.
                            if (string.IsNullOrEmpty(queueMessage.MessageText) || queueMessage.DequeueCount > _options.MaxDequeueCount + 1)
                            {
                                _logger.Error("[DeleteMessage] Message is empty or invalid format. MsgContent: {MsgContent}", queueMessage.MessageText);
                                await _queue.DeleteMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt);
                                continue;
                            }

                            waitTime = GetWaitTime(true);
                            var messageText = queueMessage.MessageText;

                            // The message added by the classic SDK is in base64 format, while new SDK will not encode it.
                            if (messageText.IsBase64())
                            {
                                messageText = messageText.FromBase64();
                            }

                            var message = messageText.FromJson<LiftrQueueMessage>();
                            message.InsertedOn = queueMessage.InsertedOn;
                            message.ExpiresOn = queueMessage.ExpiresOn;
                            message.DequeueCount = queueMessage.DequeueCount;
                            using var msgIdScope = new LogContextPropertyScope("LiftrQueueMessageId", message.MsgId);

                            if (QueueMessageVersion.v2.StrictEquals(message.Version))
                            {
                                message.Content = message.Content.FromBase64();
                            }

                            if (queueMessage.DequeueCount > _options.MaxDequeueCount)
                            {
                                var endTme = _timeSource.UtcNow;
                                var duration = endTme - message.CreatedAt.ParseZuluDateTime();

                                _logger.Information(
                                    "[DeleteMessage] Message exceeded the max dequeue count. DurationInSeconds: {DurationInSeconds}, DequeueCount '{DequeueCount}', CreatedAt '{CreatedAt}', InsertedOn '{InsertedOn}', ExpiresOn '{ExpiresOn}'",
                                    duration.TotalSeconds,
                                    message.DequeueCount,
                                    message.CreatedAt,
                                    message.InsertedOn,
                                    message.ExpiresOn);

                                await _queue.DeleteMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt);
                                continue;
                            }

                            string correlationId = null;
                            LogEventLevel? overrideLevel = null;
                            if (message.MsgTelemetryContext != null)
                            {
                                CallContextHolder.ClientRequestId.Value = message.MsgTelemetryContext.ClientRequestId;
                                CallContextHolder.ARMRequestTrackingId.Value = message.MsgTelemetryContext.ARMRequestTrackingId;
                                CallContextHolder.CorrelationId.Value = message.MsgTelemetryContext.CorrelationId;
                                correlationId = message.MsgTelemetryContext.CorrelationId;

                                if (Enum.TryParse<LogEventLevel>(message.MsgTelemetryContext.LogFilterOverwrite, true, out var level))
                                {
                                    overrideLevel = level;

                                    // Pass the log level filter to the next tier.
                                    CallContextHolder.LogFilterOverwrite.Value = message.MsgTelemetryContext.LogFilterOverwrite;
                                }
                            }

                            try
                            {
                                using (var lease = new QueueMessageLeaseScope(_queue, queueMessage, _messageLeaseRenewTime, _visibilityTime, _logger))
                                using (var logFilterOverrideScope = new LogFilterOverrideScope(overrideLevel))
                                using (new LogContextPropertyScope("LiftrClientReqId", message.MsgTelemetryContext?.ClientRequestId))
                                using (new LogContextPropertyScope("LiftrTrackingId", message.MsgTelemetryContext?.ARMRequestTrackingId))
                                using (new LogContextPropertyScope("LiftrCorrelationId", message.MsgTelemetryContext?.CorrelationId))
                                using (new LogContextPropertyScope("ARMOperationName", message.MsgTelemetryContext?.ARMOperationName))
                                using (var operation = _logger.StartTimedOperation("ProcessQueueMessage", correlationId))
                                {
                                    operation.WithLabel("ARMOperationName", message.MsgTelemetryContext?.ARMOperationName ?? "Unknown", setContextProperty: false);

                                    operation.SetProperty(nameof(srpMsgId), srpMsgId);
                                    operation.SetProperty(nameof(message.MsgId), message.MsgId);
                                    _logger.Information(
                                        "Queue msg info: DequeueCount '{DequeueCount}', CreatedAt '{CreatedAt}', InsertedOn '{InsertedOn}', ExpiresOn '{ExpiresOn}'",
                                        message.DequeueCount,
                                        message.CreatedAt,
                                        message.InsertedOn,
                                        message.ExpiresOn);

                                    var processingResult = new QueueMessageProcessingResult();
                                    try
                                    {
                                        await messageProcessingCallback(message, processingResult, cancellationToken);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.Error(ex, $"{nameof(messageProcessingCallback)} threw unhandled exception.");
                                        processingResult.SuccessfullyProcessed = false;
                                        processingResult.ProcessingError = ex.Message;
                                    }
                                    finally
                                    {
                                        bool deleteMessage = false;
                                        var endTme = _timeSource.UtcNow;
                                        var duration = endTme - message.CreatedAt.ParseZuluDateTime();

                                        if (processingResult.SuccessfullyProcessed)
                                        {
                                            _logger.Information(
                                                "[DeleteMessage] Finished processing queue message. DurationInSeconds: {DurationInSeconds}, CreatedAt:{CreatedAt}, FinishedAt: {FinishedAt}",
                                                duration.TotalSeconds,
                                                message.CreatedAt,
                                                endTme.ToZuluString());

                                            deleteMessage = true;
                                        }
                                        else if (queueMessage.DequeueCount >= _options.MaxDequeueCount)
                                        {
                                            _logger.Information(
                                                "[DeleteMessage] Failed processing queue message at max dequeue count. DurationInSeconds: {DurationInSeconds}, DequeueCount '{DequeueCount}', CreatedAt '{CreatedAt}', InsertedOn '{InsertedOn}', ExpiresOn '{ExpiresOn}'",
                                                duration.TotalSeconds,
                                                message.DequeueCount,
                                                message.CreatedAt,
                                                message.InsertedOn,
                                                message.ExpiresOn);

                                            deleteMessage = true;
                                        }

                                        if (!processingResult.SuccessfullyProcessed)
                                        {
                                            operation.FailOperation(processingResult.ProcessingError);
                                        }

                                        if (deleteMessage)
                                        {
                                            await lease.SyncMutex.WaitAsync(cancellationToken);
                                            try
                                            {
                                                await _queue.DeleteMessageAsync(queueMessage.MessageId, lease.PopReceipt);
                                            }
                                            finally
                                            {
                                                lease.SyncMutex.Release();
                                            }
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
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Issue happend at processing message from queue.");
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
            // This '_waitTime' is the shared state between multiple workers (threads).
            // Lock the '_waitTimeLock' to make sure all the '_waitTime' access is in the same critical section.
            lock (_waitTimeLock)
            {
                if (processedMessage)
                {
                    _waitTime = QueueParameters.ScanMinWaitTime;
                }
                else
                {
                    // Exponential backoff.
                    _waitTime = TimeSpan.FromMilliseconds(1.7 * _waitTime.TotalMilliseconds);
                    if (_waitTime > QueueParameters.ScanMaxWaitTime)
                    {
                        _waitTime = QueueParameters.ScanMaxWaitTime;
                    }
                }
            }

            return _waitTime;
        }

        private async Task<QueueMessage> GetQueueMessageAsync(CancellationToken cancellationToken)
        {
            using var noAppInsightsTelemetryScope = new NoAppInsightsScope();
            var messages = (await _queue.ReceiveMessagesAsync(maxMessages: 1, visibilityTimeout: _visibilityTime, cancellationToken: cancellationToken)).Value;
            var queueMessage = messages?.FirstOrDefault();
            return queueMessage;
        }
    }
}
