//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Queues;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
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
        private const string c_throwMsg = "Throw message";
        private readonly ITestOutputHelper _output;

        public WriteReadQueueTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task CanEnqueueAndDequeueAsync()
        {
            var ts = new MockTimeSource();
            using (var semophore = new SemaphoreSlim(0, 1))
            using (var scope = new TestResourceGroupScope(SdkContext.RandomResourceName("q", 15), _output))
            {
                try
                {
                    var storageAccount = await scope.GetTestStorageAccountAsync();
                    var queueUri = new Uri($"https://{storageAccount.Name}.queue.core.windows.net/myqueue");
                    QueueClient queue = new QueueClient(queueUri, TestCredentials.TokenCredential);
                    await queue.CreateAsync();

                    var writer = new QueueWriter(queue, ts, scope.Logger);

                    List<LiftrQueueMessage> receivedMessages = new List<LiftrQueueMessage>();

                    Func<LiftrQueueMessage, QueueMessageProcessingResult, CancellationToken, Task> func = async (msg, result, cancellationToken) =>
                    {
                        receivedMessages.Add(msg);
                        await Task.Yield();
                        if (msg.Content.OrdinalEquals(c_throwMsg) && msg.DequeueCount == 1)
                        {
                            result.SuccessfullyProcessed = false;
                            ts.Add(TimeSpan.FromSeconds(60));
                            await Task.Delay(TimeSpan.FromSeconds(60.0));
                        }
                        else
                        {
                            ts.Add(TimeSpan.FromSeconds(30));
                            await Task.Delay(TimeSpan.FromSeconds(15.0));
                        }

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
                        Assert.NotNull(msg.MsgTelemetryContext);
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

                    // The first process will fail.
                    await writer.AddMessageAsync(c_throwMsg);

                    // Wait for the first process.
                    await semophore.WaitAsync();

                    // Wait for the second process.
                    await Task.Delay(TimeSpan.FromSeconds(120));

                    Assert.Equal(4, receivedMessages.Count);
                    {
                        var msg = receivedMessages.Last();
                        Assert.NotNull(msg.MsgTelemetryContext);
                        Assert.False(string.IsNullOrEmpty(msg.MsgTelemetryContext.CorrelationId));
                        Assert.Equal(c_throwMsg, msg.Content);
                    }

                    Assert.Null(reader.ReaderException);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed.");
                    scope.TimedOperation.FailOperation(ex.Message);
                    throw;
                }
            }
        }
    }
}
