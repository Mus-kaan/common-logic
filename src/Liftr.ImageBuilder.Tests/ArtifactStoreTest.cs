//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using System;
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
            MockTimeSource timeSource = new MockTimeSource();
            var namingContext = new NamingContext("ArtifactStore", "arti", EnvironmentType.Test, TestCommon.Location);
            TestCommon.AddCommonTags(namingContext.Tags);
            var baseName = SdkContext.RandomResourceName(string.Empty, 20).Substring(0, 8);

            var artifactOptions = new ArtifactStoreOptions()
            {
                ContainerName = "artifacts",
                OldArtifactTTLInDays = 7,
            };

            using (var scope = new TestResourceGroupScope(baseName, namingContext, _output))
            {
                try
                {
                    var storageAccount = await scope.GetTestStorageAccountAsync();

                    string blobEndpoint = $"https://{storageAccount.Name}.blob.core.windows.net";
                    var cred = new ClientSecretCredential(TestCredentials.TenantId, TestCredentials.ClientId, TestCredentials.ClientSecret);
                    BlobServiceClient blobClient = new BlobServiceClient(new Uri(blobEndpoint), cred);
                    var containerClient = (await blobClient.CreateBlobContainerAsync(artifactOptions.ContainerName)).Value;
                    var key = (await blobClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7))).Value;

                    var store = new ArtifactStore(
                        storageAccount.Name,
                        key,
                        containerClient,
                        artifactOptions,
                        timeSource,
                        scope.Logger);

                    for (int i = 0; i < 5; i++)
                    {
                        await store.UploadBuildArtifactsAndGenerateReadSASAsync("packer.tar");
                        timeSource.Add(TimeSpan.FromSeconds(123));
                    }

                    timeSource.Add(TimeSpan.FromDays(40));
                    var deletedCount = await store.CleanUpOldArtifactsAsync();

                    Assert.Equal(5, deletedCount);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed");
                    throw;
                }
            }
        }
    }
}
