//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Threading.Tasks;
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

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task CanStorageAccountAsync()
        {
            // This test will normally take about 12 minutes.
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
                    var connectionStr = await st.GetPrimaryConnectionStringAsync();
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
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed.");
                    throw;
                }
            }
        }
    }
}
