//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Queues;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Identity;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Queue.Tests
{
    public class TokenCredentialCacheTests
    {
        private readonly ITestOutputHelper _output;

        public TokenCredentialCacheTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task CacheCanUsedForQueueAsync()
        {
            var ts = new MockTimeSource();
            using (var semaphore = new SemaphoreSlim(0, 1))
            using (var scope = new TestResourceGroupScope(SdkContext.RandomResourceName("q", 15), _output))
            {
                try
                {
                    var storageAccount = await scope.GetTestStorageAccountAsync();
                    using var tokenCache = new TokenCredentialCache(TestCredentials.TokenCredential);

                    for (int i = 0; i < 20; i++)
                    {
                        var queueUri = new Uri($"https://{storageAccount.Name}.queue.core.windows.net/myqueue" + i);
                        QueueClient queue = new QueueClient(queueUri, tokenCache);
                        await queue.CreateIfNotExistsAsync();
                        await queue.SendMessageAsync("test message");
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        var queueUri = new Uri($"https://{storageAccount.Name}.queue.core.windows.net/syncmyqueue" + i);
                        QueueClient queue = new QueueClient(queueUri, tokenCache);
                        queue.CreateIfNotExists();
                        queue.SendMessage("test message");
                    }
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
