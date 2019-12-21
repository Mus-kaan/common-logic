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
            Assert.True(File.Exists(Path.Combine(dir, "1_global", "ServiceModel.DogFood.json")));
            Assert.True(File.Exists(Path.Combine(dir, "1_global", "RolloutSpec.DogFood.Global.json")));
            Assert.True(File.Exists(Path.Combine(dir, "1_global", "parameters", "DogFood", "RolloutParameters.DogFood.global.json")));
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
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "ServiceModel.TestBaseImage.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutSpec.TestBaseImage.json")));
            Assert.True(File.Exists(Path.Combine(dir, "image_builder", "RolloutParameters.TestBaseImage.json")));
        }
    }
}
