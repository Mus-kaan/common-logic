//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Logging.Blob;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class StorageAccountTests
    {
        private readonly ITestOutputHelper _output;

        public StorageAccountTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task CreateAndUpdateStorageAccountAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-st-", _output))
            {
                try
                {
                    var logger = scope.Logger;
                    var azFactory = scope.AzFactory;
                    var az = scope.Client;
                    var rg = await az.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("st", 15);

                    var vnet = await az.GetOrCreateVNetAsync(TestCommon.Location, scope.ResourceGroupName, SdkContext.RandomResourceName("vnet", 9), TestCommon.Tags);
                    var subnet = vnet.Subnets.FirstOrDefault().Value;

                    var st = await az.GetOrCreateStorageAccountAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags, subnet?.Inner?.Id);
                    st = await st.RemoveUnusedVNetRulesAsync(azFactory, logger);

                    var currentPublicIP = await MetadataHelper.GetPublicIPAddressAsync();
                    await st.WithAccessFromIpAddressAsync(currentPublicIP, logger);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed.");
                    scope.TimedOperation.FailOperation(ex.Message);
                    throw;
                }
            }
        }

        [CheckInValidation(skipLinux: true)]
        public async Task RotateStorageAccountCredentialAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-st-", _output))
            {
                try
                {
                    var ts = new MockTimeSource();
                    var logger = scope.Logger;
                    var azFactory = scope.AzFactory;
                    var az = scope.Client;
                    var rg = await az.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("st", 15);

                    var st = await az.GetOrCreateStorageAccountAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);
                    var originalKeys = await st.GetKeysAsync();
                    var originalConn1 = originalKeys.FirstOrDefault(k => k.KeyName.OrdinalEquals("key1")).ToConnectionString(st.Name);
                    var originalConn2 = originalKeys.FirstOrDefault(k => k.KeyName.OrdinalEquals("key2")).ToConnectionString(st.Name);

                    var rotationManager = new StorageAccountCredentialLifeCycleManager(st, ts, logger);

                    // The primary is the default active.
                    var conn = await rotationManager.GetActiveConnectionStringAsync();
                    Assert.Equal(originalConn1, conn);

                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    Assert.Equal(originalConn1, conn);

                    ts.Add(TimeSpan.FromDays(2.3));
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    Assert.Equal(originalConn1, conn);

                    ts.Add(TimeSpan.FromDays(2.3));
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    Assert.Equal(originalConn1, conn);

                    // make sure both are not rotated
                    var keys = await st.GetKeysAsync();
                    var connectionString1 = keys.FirstOrDefault(k => k.KeyName.OrdinalEquals("key1")).ToConnectionString(st.Name);
                    var connectionString2 = keys.FirstOrDefault(k => k.KeyName.OrdinalEquals("key2")).ToConnectionString(st.Name);

                    Assert.Equal(originalConn1, connectionString1);
                    Assert.Equal(originalConn2, connectionString2);

                    // This will trigger rotation
                    ts.Add(TimeSpan.FromDays(28));
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    keys = await st.GetKeysAsync();
                    connectionString1 = keys.FirstOrDefault(k => k.KeyName.OrdinalEquals("key1")).ToConnectionString(st.Name);
                    connectionString2 = keys.FirstOrDefault(k => k.KeyName.OrdinalEquals("key2")).ToConnectionString(st.Name);

                    // active is secondary.
                    Assert.Equal(connectionString2, conn);

                    // conn1 is not rotated.
                    Assert.Equal(originalConn1, connectionString1);

                    // conn2 is rotated.
                    Assert.NotEqual(originalConn2, connectionString2);

                    // in TTL, not rotate
                    ts.Add(TimeSpan.FromDays(2.3));
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    Assert.Equal(connectionString2, conn);

                    // This will trigger rotation again
                    ts.Add(TimeSpan.FromDays(28));
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    keys = await st.GetKeysAsync();
                    var lastConnectionString1 = keys.FirstOrDefault(k => k.KeyName.OrdinalEquals("key1")).ToConnectionString(st.Name);
                    var lastConnectionString2 = keys.FirstOrDefault(k => k.KeyName.OrdinalEquals("key2")).ToConnectionString(st.Name);

                    // active is primary.
                    Assert.Equal(lastConnectionString1, conn);

                    // conn1 is rotated.
                    Assert.NotEqual(connectionString1, lastConnectionString1);

                    // conn2 is not rotated.
                    Assert.Equal(connectionString2, lastConnectionString2);

                    // explicit rotate
                    ts.Add(TimeSpan.FromDays(2.3));
                    await rotationManager.RotateCredentialAsync();
                    conn = await rotationManager.GetActiveConnectionStringAsync();
                    keys = await st.GetKeysAsync();
                    var explicitConnectionString1 = keys.FirstOrDefault(k => k.KeyName.OrdinalEquals("key1")).ToConnectionString(st.Name);
                    var explicitConnectionString2 = keys.FirstOrDefault(k => k.KeyName.OrdinalEquals("key2")).ToConnectionString(st.Name);

                    // active is secondary.
                    Assert.Equal(explicitConnectionString2, conn);

                    // conn1 is not rotated.
                    Assert.Equal(explicitConnectionString1, lastConnectionString1);

                    // conn2 is rotated.
                    Assert.NotEqual(explicitConnectionString2, lastConnectionString2);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed.");
                    scope.TimedOperation.FailOperation(ex.Message);
                    throw;
                }
            }
        }

        [CheckInValidation(skipLinux: true)]
        public async Task CanStorageAccountAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-stor-", _output))
            {
                try
                {
                    var az = scope.Client;
                    var rg = await az.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("st", 15);
                    var st = await az.GetOrCreateStorageAccountAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);
                    var containerName1 = "test-containername1";

                    // The kv FPA object Id is no configured programatically for now.
                    // await az.DelegateStorageKeyOperationToKeyVaultAsync(rg);
                    // await az.DelegateStorageKeyOperationToKeyVaultAsync(st);
#pragma warning disable CS0618 // Type or member is obsolete
                    var connectionStr = await st.GetPrimaryConnectionStringAsync();
#pragma warning restore CS0618 // Type or member is obsolete
                    var stor = CloudStorageAccount.Parse(connectionStr);
                    CloudBlobClient blobClient = stor.CreateCloudBlobClient();
                    CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName1);
                    await blobContainer.CreateIfNotExistsAsync();

                    await az.GrantBlobContainerReaderAsync(st, containerName1, az.SPNObjectId);
                    await az.GrantBlobContainerContributorAsync(st, containerName1, az.SPNObjectId);
                    await az.GrantBlobContributorAsync(rg, az.SPNObjectId);

                    var msi1 = await az.CreateMSIAsync(TestCommon.Location, scope.ResourceGroupName, SdkContext.RandomResourceName("msi", 15), TestCommon.Tags);
                    var msi2 = await az.CreateMSIAsync(TestCommon.Location, scope.ResourceGroupName, SdkContext.RandomResourceName("msi", 15), TestCommon.Tags);
                    var msi3 = await az.CreateMSIAsync(TestCommon.Location, scope.ResourceGroupName, SdkContext.RandomResourceName("msi", 15), TestCommon.Tags);
                    var msi4 = await az.CreateMSIAsync(TestCommon.Location, scope.ResourceGroupName, SdkContext.RandomResourceName("msi", 15), TestCommon.Tags);

                    await az.GrantBlobContributorAsync(rg, msi1);
                    await az.GrantBlobContainerContributorAsync(st, containerName1, msi2);
                    await az.GrantBlobContainerContributorAsync(st, containerName1, msi3);
                    await az.GrantQueueContributorAsync(st, msi3);

                    // Test blob log store.
                    {
                        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionStr);
                        var container = blobServiceClient.GetBlobContainerClient("log-container");
                        var ts = new MockTimeSource();
                        ILogStore logStore = new BlobLogStore(container, ts, scope.Logger);
                        var logUri = await logStore.UploadLogAsync(Guid.NewGuid().ToString());
                        scope.Logger.Information("The actual log content is uploaded to blob at: {logContentUri}", logUri.ToString());
                        Assert.True(logUri.ToString().OrdinalEndsWith("blob.core.windows.net/log-container/2019-01/20/2019-01-20T08_00_00.0000000Z.txt"));
                    }

                    await st.WithAccessFromIpAddressAsync("52.183.60.248", scope.Logger);
                    await st.WithAccessFromIpAddressAsync("52.183.60.248", scope.Logger);
                    await st.WithAccessFromIpAddressAsync("52.183.60.249", scope.Logger);
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
