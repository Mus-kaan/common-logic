//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using System;
using System.Collections.Generic;
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

                    var cred = new ClientSecretCredential(TestCredentials.TenantId, TestCredentials.ClientId, TestCredentials.ClientSecret);
                    BlobServiceClient blobClient = new BlobServiceClient(new Uri(storageAccount.Inner.PrimaryEndpoints.Blob), cred);

                    var store = new ContentStore(
                        blobClient,
                        artifactOptions,
                        timeSource,
                        scope.Logger);

                    for (int i = 1; i <= 5; i++)
                    {
                        var sas = await store.UploadBuildArtifactsToSupportingStorageAsync("packer-files-ub18.zip");
                        timeSource.Add(TimeSpan.FromSeconds(123));
                        var createAt = timeSource.UtcNow.ToZuluString();
                        var deletedAt = timeSource.UtcNow.AddDays(artifactOptions.ContentTTLInDays).ToZuluString();
                        await store.CopyVHDToExportAsync(sas, "TestImageName", "1.2.1" + i, SourceImageType.U1804LTS, (IReadOnlyDictionary<string, string>)namingContext.Tags, createAt, deletedAt);
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
