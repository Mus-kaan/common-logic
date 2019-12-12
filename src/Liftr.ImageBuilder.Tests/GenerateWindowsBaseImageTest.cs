//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
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
    public class GenerateWindowsBaseImageTest
    {
        private readonly ITestOutputHelper _output;

        public GenerateWindowsBaseImageTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task VerifyWindowsBaseImageGenerationAsync()
        {
            MockTimeSource timeSource = new MockTimeSource();
            var namingContext = new NamingContext("ImageBuilder", "www", EnvironmentType.Test, TestCommon.Location);
            TestCommon.AddCommonTags(namingContext.Tags);
            var baseName = SdkContext.RandomResourceName(string.Empty, 20).Substring(0, 8);

            using (var scope = new TestResourceGroupScope(baseName, _output))
            {
                var orchestrator = new ImageBuilderOrchestrator(scope.AzFactory, timeSource, scope.Logger);

                ImageBuilderOptions imgOptions = new ImageBuilderOptions()
                {
                    ResourceGroupName = namingContext.ResourceGroupName(baseName),
                    GalleryName = namingContext.SharedImageGalleryName(baseName),
                    ImageDefinitionName = "TestImageDefinition",
                    StorageAccountName = namingContext.StorageAccountName(baseName),
                    Location = namingContext.Location,
                    Tags = new Dictionary<string, string>(namingContext.Tags),
                    ImageVersionTTLInDays = 15,
                };

                ArtifactStoreOptions artifactOptions = new ArtifactStoreOptions()
                {
                    ContainerName = "artifacts",
                };

                try
                {
                    await orchestrator.CreateOrUpdateInfraAsync(
                                    imgOptions,
                                    TestCredentials.AzureVMImageBuilderObjectIdAME,
                                    namingContext.KeyVaultName(baseName),
                                    false);

                    var result = await orchestrator.BuildCustomizedSBIAsync(
                                    imgOptions,
                                    artifactOptions,
                                    "packer-windows.tar.gz",
                                    "0.9.01018.0002-3678b756",
                                    "2019.0.20190214",
                                    false,
                                    CancellationToken.None);
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
