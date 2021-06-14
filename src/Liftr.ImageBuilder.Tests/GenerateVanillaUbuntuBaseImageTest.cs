//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class GenerateVanillaUbuntuBaseImageTest : LiftrAzureTestBase
    {
        public GenerateVanillaUbuntuBaseImageTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [JenkinsOnly]
        [PublicEastUS]
        public async Task VerifySBIGenerationAsync()
        {
            MockTimeSource timeSource = new MockTimeSource();
            var tags = new Dictionary<string, string>();
            TestCommon.AddCommonTags(tags);
            var baseName = SdkContext.RandomResourceName(string.Empty, 20).Substring(0, 8);

            try
            {
                var options = new BuilderOptions()
                {
                    SubscriptionId = new Guid(TestCredentials.SubscriptionId),
                    Location = Location,
                    ResourceGroupName = ResourceGroupName,
                    ImageGalleryName = "ubsigtest" + baseName,
                    ImageReplicationRegions = new List<Region>()
                    {
                        Region.USEast,
                    },
                    KeepAzureVMImageBuilderLogs = false,
                    ExportVHDToStorage = true,
                };

                var orchestrator = new ImageBuilderOrchestrator(options, AzFactory, TestCredentials.KeyVaultClient, timeSource, Logger);

                (var kv, var gallery, var artifactStore, var stor) = await orchestrator.CreateOrUpdateLiftrImageBuilderInfrastructureAsync(InfrastructureType.BakeNewImageAndExport, SourceImageType.UbuntuServer1804, tags: tags);
                Assert.NotNull(kv);

                await orchestrator.BuildCustomizedSBIAsync(
                                "img" + baseName,
                                "0.9.1018",
                                SourceImageType.UbuntuServer1804,
                                "packer-files-ub18.zip",
                                tags,
                                CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }
        }
    }
}
