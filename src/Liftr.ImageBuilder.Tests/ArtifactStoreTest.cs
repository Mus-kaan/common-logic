//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class ArtifactStoreTest
    {
        private readonly ITestOutputHelper _output;

        public ArtifactStoreTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task VerifyArtifactsCleanUpAsync()
        {
            MockTimeSource ts = new MockTimeSource();
            var namingContext = new NamingContext("ArtifactStore", "arti", EnvironmentType.Test, TestCommon.Location);
            TestCommon.AddCommonTags(namingContext.Tags);
            var baseName = SdkContext.RandomResourceName(string.Empty, 20).Substring(0, 8);

            var ops = new ArtifactStoreOptions()
            {
                ContainerName = "artifacts",
                OldArtifactTTLInDays = 7,
            };

            using (var scope = new TestResourceGroupScope(baseName, namingContext, _output))
            {
                try
                {
                    var az = scope.AzFactory.GenerateLiftrAzure();
                    await az.CreateResourceGroupAsync(namingContext.Location, namingContext.ResourceGroupName(baseName), namingContext.Tags);
                    var stor = await az.CreateStorageAccountAsync(namingContext.Location, namingContext.ResourceGroupName(baseName), namingContext.StorageAccountName(baseName), namingContext.Tags);
                    var storageAccount = await GetAccountAsync(stor);

                    var store = new ArtifactStore(ops, ts, scope.Logger);

                    for (int i = 0; i < 5; i++)
                    {
                        await store.UploadBuildArtifactsAndGenerateReadSASAsync(storageAccount, "packer.tar");
                        ts.Add(TimeSpan.FromSeconds(123));
                    }

                    ts.Add(TimeSpan.FromDays(40));
                    var deletedCount = await store.CleanUpOldArtifactsAsync(storageAccount);

                    Assert.Equal(5, deletedCount);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed");
                    throw;
                }
            }
        }

        private async Task<CloudStorageAccount> GetAccountAsync(IStorageAccount storageAccount)
        {
            var keys = await storageAccount.GetKeysAsync();
            var key = keys[0];
            var cred = new StorageCredentials(storageAccount.Name, key.Value, key.KeyName);
            return new CloudStorageAccount(cred, useHttps: true);
        }
    }
}
