//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

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
    public class ResourceProvisioningTest
    {
        private readonly ITestOutputHelper _output;

        public ResourceProvisioningTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task VerifyResourcesProvisioningAsync()
        {
            MockTimeSource ts = new MockTimeSource();
            var namingContext = new NamingContext("ImageBuilder", "img", EnvironmentType.Test, TestCommon.Location);
            TestCommon.AddCommonTags(namingContext.Tags);
            var baseName = SdkContext.RandomResourceName(string.Empty, 20).Substring(0, 8);

            using (var scope = new TestResourceGroupScope(baseName, _output))
            {
                var orchestrator = new ImageBuilderOrchestrator(scope.AzFactory, ts, scope.Logger);

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

                try
                {
                    await orchestrator.CreateOrUpdateInfraAsync(
                                    imgOptions,
                                    TestCredentials.AzureVMImageBuilderObjectIdAME,
                                    namingContext.KeyVaultName(baseName));

                    // Run another time will not fail.
                    await orchestrator.CreateOrUpdateInfraAsync(
                                    imgOptions,
                                    TestCredentials.AzureVMImageBuilderObjectIdAME,
                                    namingContext.KeyVaultName(baseName));
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
