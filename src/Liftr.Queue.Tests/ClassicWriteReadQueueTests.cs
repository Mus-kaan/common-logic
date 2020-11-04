//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Queues;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Liftr.ClassicQueue;
using Microsoft.Liftr.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Queue.Tests
{
    public class ClassicWriteReadQueueTests
    {
        private const string c_throwMsg = "Throw message";
        private readonly ITestOutputHelper _output;

        public ClassicWriteReadQueueTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task CanEnqueueAndDequeueAsync()
        {
            var ts = new MockTimeSource();
            using (var semaphore = new SemaphoreSlim(0, 1))
            using (var scope = new TestResourceGroupScope(SdkContext.RandomResourceName("q", 15), _output))
            {
                try
                {
                    const string queueName = "myqueue";
                    var storageAccount = await scope.GetTestStorageAccountAsync();
                    var queueUri = new Uri($"https://{storageAccount.Name}.queue.core.windows.net/{queueName}");
                    QueueClient queue = new QueueClient(queueUri, TestCredentials.TokenCredential);
                    await queue.CreateAsync();

                    IQueueWriter writer = null;
                    {
                        var connectionString = await storageAccount.GetPrimaryConnectionStringAsync();
                        Azure.Storage.CloudStorageAccount classicStorageAccount = Azure.Storage.CloudStorageAccount.Parse(connectionString);
                        CloudQueueClient queueClient = classicStorageAccount.CreateCloudQueueClient();
                        var classicQueue = queueClient.GetQueueReference(queueName);
                        writer = new ClassicQueueWriter(classicQueue, ts, scope.Logger);
                    }

                    List<LiftrQueueMessage> receivedMessages = new List<LiftrQueueMessage>();

                    Func<LiftrQueueMessage, QueueMessageProcessingResult, CancellationToken, Task> func = async (msg, result, cancellationToken) =>
                    {
                        receivedMessages.Add(msg);

                        // make sure the other task will be scheduled.
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

                        semaphore.Release();
                    };

                    var reader = new QueueReader(queue, new QueueReaderOptions() { MaxConcurrentCalls = 1 }, ts, scope.Logger);
                    var readerTask = reader.StartListeningAsync(func);

                    var msgContent1 = "Hello!!";

                    await writer.AddMessageAsync(msgContent1);

                    await semaphore.WaitAsync();

                    // make sure the other task will be scheduled.
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

                    await semaphore.WaitAsync();

                    // make sure the other task will be scheduled.
                    await Task.Yield();

                    Assert.Equal(2, receivedMessages.Count);
                    {
                        var msg = receivedMessages.Last();
                        Assert.NotNull(msg.MsgTelemetryContext);
                        Assert.False(string.IsNullOrEmpty(msg.MsgTelemetryContext.CorrelationId));
                        Assert.Equal(msgContent1, msg.Content);
                        Assert.Equal("2019-01-20T08:01:00.0000000Z", msg.CreatedAt);
                    }

                    // The first process will fail and will be reprocessed.
                    await writer.AddMessageAsync(c_throwMsg);

                    // Wait for the first process.
                    await semaphore.WaitAsync();

                    // Wait for the second process. Make this longer to test message lease.
                    await Task.Delay(TimeSpan.FromSeconds(120));

                    // The failed message will be recorded twice.
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
