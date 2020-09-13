//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.KeyVault;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class ImportWindowsBaseImageTest
    {
        private readonly ITestOutputHelper _output;

        public ImportWindowsBaseImageTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task VerifyImportWindowsAsync()
        {
            MockTimeSource timeSource = new MockTimeSource();
            var tags = new Dictionary<string, string>();
            TestCommon.AddCommonTags(tags);
            var baseName = SdkContext.RandomResourceName(string.Empty, 20).Substring(0, 8);

            using (var scope = new TestResourceGroupScope(baseName, _output))
            {
                var options = new BuilderOptions()
                {
                    SubscriptionId = new Guid(TestCredentials.SubscriptionId),
                    Location = TestCommon.Location,
                    ResourceGroupName = scope.ResourceGroupName,
                    ImageGalleryName = "Import" + baseName,
                    ImageReplicationRegions = new List<Region>()
                    {
                        Region.USEast,
                    },
                };

                var orchestrator = new ImageBuilderOrchestrator(options, scope.AzFactory, TestCredentials.KeyVaultClient, timeSource, scope.Logger);

                try
                {
                    (var kv, var gallery, var artifactStore, var stor) = await orchestrator.CreateOrUpdateLiftrImageBuilderInfrastructureAsync(InfrastructureType.ImportImage, sourceImageType: null, tags: tags);

                    using (var sharedTestKvValet = new KeyVaultConcierge(TestCredentials.SharedKeyVaultUri, TestCredentials.KeyVaultClient, scope.Logger))
                    using (var kvValet = new KeyVaultConcierge(kv.VaultUri, TestCredentials.KeyVaultClient, scope.Logger))
                    {
                        var connStr = (await sharedTestKvValet.GetSecretAsync(ImageImporter.c_exportingStorageAccountConnectionStringSecretName)).Value;
                        await kvValet.SetSecretAsync(ImageImporter.c_exportingStorageAccountConnectionStringSecretName, connStr);

                        var importer = new ImageImporter(options, artifactStore, scope.AzFactory, kvValet, timeSource, scope.Logger);
                        await importer.ImportImageVHDAsync("LiftrUTWinImg", "0.5.666");

                        // Import again will not fail.
                        await importer.ImportImageVHDAsync("LiftrUTWinImg", "0.5.666");
                    }
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, ex.Message);
                    throw;
                }
            }
        }
    }
}
