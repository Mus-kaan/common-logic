//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.EV2.Tests
{
    public sealed class EV2ArtifactsGeneratorTests
    {
        private readonly Serilog.ILogger _logger;

        public EV2ArtifactsGeneratorTests(ITestOutputHelper output)
        {
            _logger = TestLogger.GenerateLogger(output);
        }

        [Fact]
        public void VerifyGenerateArtifacts()
        {
            var artifact = new EV2ArtifactsGenerator(_logger);

            var ev2Options = JsonConvert.DeserializeObject<EV2HostingOptions>(File.ReadAllText("TestEv2HostingOptions.json"));

            var dir = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());

            artifact.GenerateArtifacts(ev2Options, dir);

            var folders = Directory.GetDirectories(dir);

            Assert.Equal(4, folders.Length);
            {
                var filePath = Path.Combine(dir, "1_global", "ServiceModel.DogFood.json");
                Assert.True(File.Exists(filePath), $"'{filePath}' should exist.");
            }

            {
                var filePath = Path.Combine(dir, "1_global", "RolloutSpec.DogFood.global.json");
                Assert.True(File.Exists(filePath), $"'{filePath}' should exist.");
            }

            {
                var filePath = Path.Combine(dir, "1_global", "parameters", "DogFood", "RolloutParameters.DogFood.global.json");
                Assert.True(File.Exists(filePath), $"'{filePath}' should exist.");
            }
        }

        [Fact]
        public void VerifyGenerateImageBuilderArtifacts()
        {
            var artifact = new EV2ArtifactsGenerator(_logger);

            var options = JsonConvert.DeserializeObject<EV2ImageBuilderOptions>(File.ReadAllText("TestEV2ImageOptions.json"));

            var dir = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());

            artifact.GenerateImageBuilderArtifacts(options, dir);

            var folders = Directory.GetDirectories(dir);

            Assert.Single(folders);

            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "ServiceModel.TestBaseImage-bake.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutSpec.TestBaseImage-bake.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutParameters.TestBaseImage-bake.json")));

            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "ServiceModel.TestBaseImage-dist2-Fairfax.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutSpec.TestBaseImage-dist2-Fairfax.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutParameters.TestBaseImage-dist2-Fairfax.json")));

            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "ServiceModel.TestBaseImage-dist1-Mooncake.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutSpec.TestBaseImage-dist1-Mooncake.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutParameters.TestBaseImage-dist1-Mooncake.json")));
        }

        [Fact]
        public void VerifyGenerateImageBuilderArtifactsWithRolloutName()
        {
            var artifact = new EV2ArtifactsGenerator(_logger);

            var options = JsonConvert.DeserializeObject<EV2ImageBuilderOptions>(File.ReadAllText("TestEV2ImageOptionsWithName.json"));

            var dir = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());

            artifact.GenerateImageBuilderArtifacts(options, dir);

            var folders = Directory.GetDirectories(dir);

            Assert.Single(folders);

            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "ServiceModel.BakeInPublic.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutSpec.BakeInPublic.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutParameters.BakeInPublic.json")));

            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "ServiceModel.DistributeToCanary.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutSpec.DistributeToCanary.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutParameters.DistributeToCanary.json")));

            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "ServiceModel.DistributeToProd.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutSpec.DistributeToProd.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutParameters.DistributeToProd.json")));
        }

        [Fact]
        public void VerifyUpdateErrorMessage()
        {
            var artifact = new EV2ArtifactsGenerator(_logger);

            var options = JsonConvert.DeserializeObject<EV2ImageBuilderOptions>(File.ReadAllText("UpdateEV2ImageOptions.json"));

            var dir = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());

            Assert.Throws<InvalidImageBuilderEV2OptionsException>(() =>
            {
                artifact.GenerateImageBuilderArtifacts(options, dir);
            });
        }
    }
}
