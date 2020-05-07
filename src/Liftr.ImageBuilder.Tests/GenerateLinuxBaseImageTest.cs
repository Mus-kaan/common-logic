﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.KeyVault;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class GenerateLinuxBaseImageTest
    {
        private readonly ITestOutputHelper _output;

        public GenerateLinuxBaseImageTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task VerifySBIGenerationAsync()
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
                    ImageGalleryName = "testsig" + baseName,
                    ImageReplicationRegions = new List<Region>()
                    {
                        Region.USEast,
                    },
                    KeepAzureVMImageBuilderLogs = false,
                };

                var orchestrator = new ImageBuilderOrchestrator(options, scope.AzFactory, TestCredentials.KeyVaultClient, timeSource, scope.Logger);

                try
                {
                    (var kv, _) = await orchestrator.CreateOrUpdateLiftrImageBuilderInfrastructureAsync(tags);

                    using (var testKvValet = new KeyVaultConcierge(TestCredentials.SharedKeyVaultUri, TestCredentials.KeyVaultClient, scope.Logger))
                    using (var kvValet = new KeyVaultConcierge(kv.VaultUri, TestCredentials.KeyVaultClient, scope.Logger))
                    {
                        var sbiSASToken = (await testKvValet.GetSecretAsync(ImageBuilderOrchestrator.c_SBISASSecretName)).Value;
                        await kvValet.SetSecretAsync(ImageBuilderOrchestrator.c_SBISASSecretName, sbiSASToken);
                    }

                    await orchestrator.BuildCustomizedSBIAsync(
                                    "img" + baseName,
                                    "0.9.1018",
                                    SourceImageType.U1804LTS,
                                    "packer.tar",
                                    tags,
                                    CancellationToken.None);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, ex.Message);
                    throw;
                }
            }
        }

        [Fact]
        public async Task VerifyImageVersionWillThrowAsync()
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
                    ImageGalleryName = "testsig" + baseName,
                    ImageReplicationRegions = new List<Region>()
                    {
                        Region.USEast,
                    },
                    KeepAzureVMImageBuilderLogs = false,
                };

                var orchestrator = new ImageBuilderOrchestrator(options, scope.AzFactory, TestCredentials.KeyVaultClient, timeSource, scope.Logger);

                await Assert.ThrowsAsync<InvalidImageVersionException>(
                    () =>
                    orchestrator.BuildCustomizedSBIAsync(
                        "img" + baseName,
                        "0.9.01018.0002-3678b756",
                        SourceImageType.U1804LTS,
                        "packer.tar",
                        tags,
                        CancellationToken.None));
            }
        }
    }
}
