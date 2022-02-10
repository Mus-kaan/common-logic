//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Medallion.Threading.Azure;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class LiftrAzureTestBaseLockTests : LiftrAzureTestBase
    {
        public LiftrAzureTestBaseLockTests(ITestOutputHelper output)
            : base(output, useMethodName: true)
        {
        }

        [CheckInValidation(skipLinux: true)]
        [PublicEastUS]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1008:Avoid calling System.Threading.Tasks.Task.Run()", Justification = "<Pending>")]
        public async Task VerifyLockAsync()
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(10));

            var lockName = Guid.NewGuid().ToString();

            var handle = await AcquireLockAsync(lockName, cancellationToken: cts.Token);
            await handle.DisposeAsync();

            bool task1AcquiredLock = false;
            bool task2AcquiredLock = false;
            bool task3AcquiredLock = false;

            AzureBlobLeaseDistributedLockHandle task1Handle = null;

            var task1 = Task.Run(async () =>
            {
                task1Handle = await AcquireLockAsync(lockName, cancellationToken: cts.Token);
                task1AcquiredLock = true;
            });

            // task 1 should already acquired the lock.
            await Task.Delay(5000, cts.Token);
            Assert.True(task1AcquiredLock);

            var task2 = Task.Run(async () =>
            {
                var handle = await TryAcquireLockAsync(lockName, cancellationToken: cts.Token);
                if (handle != null)
                {
                    task2AcquiredLock = true;
                }
            });

            // task 2 should not acquire the lock.
            await Task.Delay(5000, cts.Token);
            Assert.False(task2AcquiredLock);

            var task3 = Task.Run(async () =>
            {
                await AcquireLockAsync(lockName, cancellationToken: cts.Token);
                task3AcquiredLock = true;
            });

            // task 3 should not acquire the lock since task 1 is holding it.
            await Task.Delay(5000, cts.Token);
            Assert.False(task3AcquiredLock);

            // task 3 can acquire the lock after task 1 lock is released.
            await task1Handle.DisposeAsync();
            await Task.Delay(5000, cts.Token);
            Assert.True(task3AcquiredLock);
        }
    }
}
