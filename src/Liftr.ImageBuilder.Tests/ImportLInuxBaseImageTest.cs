﻿//-----------------------------------------------------------------------------
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
    public class ImportLInuxBaseImageTest
    {
        private readonly ITestOutputHelper _output;

        public ImportLInuxBaseImageTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task VerifyImportLinuxAsync()
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
                    (var kv, var artifactStore) = await orchestrator.CreateOrUpdateLiftrImageBuilderInfrastructureAsync(InfrastructureType.ImportImage, tags: tags);

                    using (var sharedTestKvValet = new KeyVaultConcierge(TestCredentials.SharedKeyVaultUri, TestCredentials.KeyVaultClient, scope.Logger))
                    using (var kvValet = new KeyVaultConcierge(kv.VaultUri, TestCredentials.KeyVaultClient, scope.Logger))
                    {
                        var connStr = (await sharedTestKvValet.GetSecretAsync(ImageImporter.c_exportingStorageAccountConnectionStringSecretName)).Value;
                        await kvValet.SetSecretAsync(ImageImporter.c_exportingStorageAccountConnectionStringSecretName, connStr);

                        var importer = new ImageImporter(options, artifactStore, scope.AzFactory, kvValet, timeSource, scope.Logger);
                        await importer.ImportImageVHDAsync("LiftrUTImg", "1.0.666");

                        // Import again will not fail.
                        await importer.ImportImageVHDAsync("LiftrUTImg", "1.0.666");
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