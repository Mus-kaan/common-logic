//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Medallion.Threading.Azure;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Tests
{
    public class LiftrAzureTestBase : LiftrTestBase
    {
        private static readonly Random s_rand = new Random();

        private static BlobContainerClient s_lockContainer = null;

        public LiftrAzureTestBase(ITestOutputHelper output, bool useMethodName = false, [CallerFilePath] string sourceFile = "")
           : base(output, useMethodName, sourceFile)
        {
            if (TestCloudType == null)
            {
                throw new InvalidOperationException("Cannot find cloud type, please make sure the test method is marked by the cloud region test trait. e.g. 'PublicWestUS2'");
            }

            if (TestAzureRegion == null)
            {
                throw new InvalidOperationException("Cannot find azure region, please make sure the test method is marked by the cloud region test trait. e.g. 'PublicWestUS2'");
            }

            TestCredentails = TestCredentailsLoader.LoadTestCredentails(TestCloudType.Value, Logger);

            AzFactory = TestCredentails.AzFactory;

            SubscriptionId = TestCredentails.SubscriptionId;

            ResourceGroupName = $"{TestClassName}-{DateTimeStr}-{s_rand.Next(0, 999)}{TestAzureRegion.ShortName}";

            Location = TestAzureRegion.ToFluentRegion();

            TestResourceGroup = Client.CreateResourceGroup(Location, ResourceGroupName, Tags);
        }

        public TestCredentails TestCredentails { get; }

        public LiftrAzureFactory AzFactory { get; }

        public ILiftrAzure Client
        {
            get
            {
                return AzFactory.GenerateLiftrAzure();
            }
        }

        public string SubscriptionId { get; }

        public string ResourceGroupName { get; }

        public IResourceGroup TestResourceGroup { get; }

        public Region Location { get; }

        public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>(TestCommon.Tags);

        public override void Dispose()
        {
            base.Dispose(); // This will find if the test failed.
            if (IsFailure == false)
            {
                // Delete the rg when the test succeeded.
                _ = Client.DeleteResourceGroupAsync(ResourceGroupName);
                Thread.Sleep(3000);
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Acquires the lock asynchronously, failing with System.TimeoutException if the attempt times out.
        /// Usage:
        /// <code>
        /// await using (await AcquireLockAsync(...))
        ///     {
        ///     /* we have the lock! */
        ///     }
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name="lockName">Name of the lock</param>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to System.Threading.Timeout.InfiniteTimeSpan</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle which can be used to release the lock</returns>
        public async Task<AzureBlobLeaseDistributedLockHandle> AcquireLockAsync(string lockName, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            using var ops = Logger.StartTimedOperation("GettingLock-" + lockName);
            try
            {
                var blobLock = await GetLockAsync(lockName);
                return await blobLock.AcquireAsync(timeout, cancellationToken);
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Attempts to acquire the lock asynchronously.
        /// Usage:
        /// <code>
        ///  await using (var handle = await TryAcquireLockAsync(...))
        ///     {
        ///     if (handle != null) { /* we have the lock! */ }
        ///     }
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name="lockName">Name of the lock</param>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to System.Threading.Timeout.InfiniteTimeSpan</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle which can be used to release the lock</returns>
        public async Task<AzureBlobLeaseDistributedLockHandle> TryAcquireLockAsync(string lockName, TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            var blobLock = await GetLockAsync(lockName);
            return await blobLock.TryAcquireAsync(timeout, cancellationToken);
        }

        public Task<AzureBlobLeaseDistributedLockHandle> AcquireTrafficManaderTestLockAsync()
        {
            return AcquireLockAsync("TrafficManagerTest");
        }

        private async Task<BlobContainerClient> GetLockStorageContainerAsync()
        {
            if (s_lockContainer != null)
            {
                return s_lockContainer;
            }

            var tags = new Dictionary<string, string>()
            {
                ["CreatedAt"] = DateTime.UtcNow.ToShortDateString(),
                ["TestRunningMachine"] = Environment.MachineName,
                ["ResourceCreationTimestamp"] = DateTime.UtcNow.ToZuluString(),
            };

            var rgName = "test-lock-rg";
            var lockStorageName = ("utlock" + SubscriptionId.Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)).Substring(0, 24);
            var blobContainerName = "lock";

            var rg = await Client.GetOrCreateResourceGroupAsync(TestCommon.Location, rgName, tags);
            var stor = await Client.GetOrCreateStorageAccountAsync(TestCommon.Location, rgName, lockStorageName, tags);

#pragma warning disable CS0618 // Type or member is obsolete
            var conn = await stor.GetPrimaryConnectionStringAsync();
#pragma warning restore CS0618 // Type or member is obsolete

            s_lockContainer = new BlobContainerClient(conn, blobContainerName);
            await s_lockContainer.CreateIfNotExistsAsync();

            return s_lockContainer;
        }

        private async Task<AzureBlobLeaseDistributedLock> GetLockAsync(string lockName)
        {
            var lockContainer = await GetLockStorageContainerAsync();
            return new AzureBlobLeaseDistributedLock(lockContainer, lockName);
        }
    }
}
