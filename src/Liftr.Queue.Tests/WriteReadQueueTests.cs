﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Queue.Tests
{
    public class WriteReadQueueTests
    {
        private readonly ITestOutputHelper _output;

        public WriteReadQueueTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanEnqueueAndDequeueAsync()
        {
            var ts = new MockTimeSource();
            using (var semophore = new SemaphoreSlim(0, 1))
            using (var scope = new TestResourceGroupScope(SdkContext.RandomResourceName("q", 15), _output))
            {
                var stor = await scope.GetTestStorageAccountAsync();
                CloudQueueClient queueClient = stor.CreateCloudQueueClient();
                CloudQueue queue = queueClient.GetQueueReference("myqueue");
                await queue.CreateIfNotExistsAsync();

                var writer = new QueueWriter(queue, ts, scope.Logger);

                List<LiftrQueueMessage> receivedMessages = new List<LiftrQueueMessage>();

                Func<LiftrQueueMessage, QueueMessageProcessingResult, CancellationToken, Task> func = async (msg, result, cancellationToken) =>
                {
                    await Task.Yield();
                    ts.Add(TimeSpan.FromSeconds(30));
                    receivedMessages.Add(msg);
                    semophore.Release();
                };

                var reader = new QueueReader(queue, new QueueReaderOptions() { MaxConcurrentCalls = 1 }, ts, scope.Logger);
                var readerTask = reader.StartListeningAsync(func);

                var msgContent1 = "Hello!!";

                await writer.AddMessageAsync(msgContent1);

                await semophore.WaitAsync();
                await Task.Yield();

                Assert.Single(receivedMessages);
                {
                    var msg = receivedMessages.Last();
                    Assert.Null(msg.MsgTelemetryContext);
                    Assert.Equal(msgContent1, msg.Content);
                    Assert.Equal("2019-01-20T08:00:00.0000000Z", msg.CreatedAt);
                }

                ts.Add(TimeSpan.FromSeconds(30));
                using (scope.Logger.StartTimedOperation($"{nameof(CanEnqueueAndDequeueAsync)}Operation"))
                {
                    await writer.AddMessageAsync(msgContent1);
                }

                await semophore.WaitAsync();
                await Task.Yield();

                Assert.Equal(2, receivedMessages.Count);
                {
                    var msg = receivedMessages.Last();
                    Assert.NotNull(msg.MsgTelemetryContext);
                    Assert.False(string.IsNullOrEmpty(msg.MsgTelemetryContext.CorrelationId));
                    Assert.Equal(msgContent1, msg.Content);
                    Assert.Equal("2019-01-20T08:01:00.0000000Z", msg.CreatedAt);
                }
            }
        }
    }
}