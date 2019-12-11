//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class CopyBaseSBITest
    {
        private readonly ITestOutputHelper _output;

        public CopyBaseSBITest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task VerifyCopyBaseAsync()
        {
            // If this unit test keeps failing, it can mean that the SBI vhd SAS token is expired. Contact SBI team to get a new one.
            var moverOptions = JsonConvert.DeserializeObject<SBIMoverOptions>(File.ReadAllText("SBIMoverOptions.json"));
            MockTimeSource timeSource = new MockTimeSource();
            var namingContext = new NamingContext("ImageBuilder", "img", EnvironmentType.Test, TestCommon.Location);
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

                try
                {
                    await orchestrator.CreateOrUpdateInfraAsync(
                                imgOptions,
                                TestCredentials.AzureVMImageBuilderObjectIdAME,
                                namingContext.KeyVaultName(baseName),
                                true);

                    await orchestrator.MoveSBIToOurStorageAsync(
                        imgOptions,
                        moverOptions,
                        TestCredentials.SharedKeyVaultResourceId,
                        TestCredentials.KeyVaultClient);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "If this unit test keeps failing, it can mean that the SBI vhd SAS token is expired. Contact SBI team to get a new one");
                    throw;
                }
            }
        }
    }
}
