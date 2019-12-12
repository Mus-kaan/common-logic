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
    public class GenerateSBITest
    {
        private const string c_srcSBIImg = "/subscriptions/60fad35b-3a47-4ca0-b691-4a789f737cea/resourceGroups/img-test-36946824-eus-rg/providers/Microsoft.Compute/galleries/img_test_36946824_eus_sig/images/U1804LTS_Vb-4/versions/0.1.118044";
        private readonly ITestOutputHelper _output;

        public GenerateSBITest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task VerifySBIGenerationAsync()
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
                                    true);

                    var result = await orchestrator.BuildCustomizedSBIImplAsync(
                                    imgOptions,
                                    artifactOptions,
                                    "packer.tar",
                                    "0.9.01018.0002-3678b756",
                                    c_srcSBIImg,
                                    true,
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
