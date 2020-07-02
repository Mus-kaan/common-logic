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
using Xunit.Abstractions;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class GenerateWindowsBaseImageTest
    {
        private readonly ITestOutputHelper _output;

        public GenerateWindowsBaseImageTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [JenkinsOnly]
        public async Task VerifyWindowsBaseImageGenerationAsync()
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
                        ImageGalleryName = "testsig" + baseName,
                        ImageReplicationRegions = new List<Region>()
                    {
                        Region.USEast,
                    },
                    };

                    var orchestrator = new ImageBuilderOrchestrator(options, scope.AzFactory, TestCredentials.KeyVaultClient, timeSource, scope.Logger);

                    await orchestrator.CreateOrUpdateLiftrImageBuilderInfrastructureAsync(InfrastructureType.BakeNewImageAndExport, SourceImageType.WindowsServer2019DatacenterCore, tags: tags);

                    await orchestrator.BuildCustomizedSBIAsync(
                                    "img" + baseName,
                                    "0.9.1018",
                                    SourceImageType.WindowsServer2019DatacenterCore,
                                    "packer-windows.tar.gz",
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
