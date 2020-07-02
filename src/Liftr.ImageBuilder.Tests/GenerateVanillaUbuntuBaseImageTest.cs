//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class GenerateVanillaUbuntuBaseImageTest
    {
        private readonly ITestOutputHelper _output;

        public GenerateVanillaUbuntuBaseImageTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [JenkinsOnly]
        public async Task VerifySBIGenerationAsync()
        {
            MockTimeSource timeSource = new MockTimeSource();
            var tags = new Dictionary<string, string>();
            TestCommon.AddCommonTags(tags);
            var baseName = SdkContext.RandomResourceName(string.Empty, 20).Substring(0, 8);

            using (var scope = new TestResourceGroupScope(baseName, _output))
            {
                try
                {
                    var options = new BuilderOptions()
                    {
                        SubscriptionId = new Guid(TestCredentials.SubscriptionId),
                        Location = TestCommon.Location,
                        ResourceGroupName = scope.ResourceGroupName,
                        ImageGalleryName = "ubsigtest" + baseName,
                        ImageReplicationRegions = new List<Region>()
                    {
                        Region.USEast,
                    },
                        KeepAzureVMImageBuilderLogs = false,
                        ExportVHDToStorage = true,
                    };

                    var orchestrator = new ImageBuilderOrchestrator(options, scope.AzFactory, TestCredentials.KeyVaultClient, timeSource, scope.Logger);

                    (var kv, _) = await orchestrator.CreateOrUpdateLiftrImageBuilderInfrastructureAsync(InfrastructureType.BakeNewImageAndExport, SourceImageType.UbuntuServer1804, tags: tags);
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
                    scope.SkipDeleteResourceGroup = true;
                    scope.TimedOperation.FailOperation(ex.Message);
                    scope.Logger.Error(ex, ex.Message);
                    throw;
                }
            }
        }
    }
}
