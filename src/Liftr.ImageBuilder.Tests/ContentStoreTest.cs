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
    public class ContentStoreTest
    {
        private readonly ITestOutputHelper _output;

        public ContentStoreTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task VerifyCleanUpAsync()
        {
            MockTimeSource timeSource = new MockTimeSource();
            var namingContext = new NamingContext("ContentStore", "con", EnvironmentType.Test, TestCommon.Location);
            TestCommon.AddCommonTags(namingContext.Tags);
            var baseName = SdkContext.RandomResourceName(string.Empty, 20).Substring(0, 8);

            var artifactOptions = new ContentStoreOptions()
            {
                ArtifactContainerName = "artifacts",
                VHDExportContainerName = "test-vhd-exporting",
                ContentTTLInDays = 7,
            };

            using (var scope = new TestResourceGroupScope(baseName, namingContext, _output))
            {
                try
                {
                    var storageAccount = await scope.GetTestStorageAccountAsync();

                    string blobEndpoint = $"https://{storageAccount.Name}.blob.core.windows.net";
                    var cred = new ClientSecretCredential(TestCredentials.TenantId, TestCredentials.ClientId, TestCredentials.ClientSecret);
                    BlobServiceClient blobClient = new BlobServiceClient(new Uri(blobEndpoint), cred);

                    var store = new ContentStore(
                        blobClient,
                        artifactOptions,
                        timeSource,
                        scope.Logger);

                    for (int i = 1; i <= 5; i++)
                    {
                        var sas = await store.UploadBuildArtifactsAndGenerateReadSASAsync("packer.tar");
                        timeSource.Add(TimeSpan.FromSeconds(123));
                        await store.CopyGeneratedVHDAsync(sas.ToString(), "TestImageName", "1.2.1" + i);
                        timeSource.Add(TimeSpan.FromSeconds(123));
                    }

                    var exportingContainerSas = (await store.GetExportingContainerSASTokenAsync()).ToString();
                    Assert.NotNull(exportingContainerSas);

                    timeSource.Add(TimeSpan.FromDays(40));
                    var deletedArtifactCount = await store.CleanUpOldArtifactsAsync();
                    var deletedVHDCount = await store.CleanUpExportingVHDsAsync();

                    Assert.Equal(5, deletedArtifactCount);
                    Assert.Equal(5, deletedVHDCount);
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
