//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;
using Microsoft.Liftr.Contracts;

namespace Microsoft.Liftr.ImageBuilder
{
    public class BuilderCommandOptions
    {
        [Option('f', "file", Required = true, HelpText = "Path to the configuration file.")]
        public string ConfigPath { get; set; }

        [Option("outputSubscriptionIdOnly", Required = false, HelpText = "Only write subscription Id to disk without any other work.")]
        public bool OutputSubscriptionIdOnly { get; set; } = false;

        [Option('n', "imageName", Required = true, HelpText = "Image name, the name of the generating Shared Image Galalery Image Name.")]
        public string ImageName { get; set; }

        [Option("imageVersionTag", Required = true, HelpText = "CDPx build tag, e.g. '0.9.01018.0002-3678b756'")]
        public string ImageVersionTag { get; set; }

        [Option("srcImg", Required = true, HelpText = "Source image type, one of: [ WindowsServer2016Datacenter, WindowsServer2016DatacenterCore, WindowsServer2016DatacenterContainers, WindowsServer2019Datacenter, WindowsServer2019DatacenterCore, WindowsServer2019DatacenterContainers, U1604LTS, U1804LTS ]")]
        public SourceImageType SourceImage { get; set; }

        [Option("spnObjectId", Required = false, HelpText = "The Object Id of the executing Service Principal.")]
        public string RunnerSPNObjectId { get; set; }

        [Option("artifactPath", Required = true, HelpText = "Path to the artifact package file. The artifact must be packed in a tar file. We will call 'bake-image.sh' in the package.")]
        public string ArtifactPath { get; set; }

        [Option("authFile", Required = false, HelpText = "Use auth file to login instead of managed identity.")]
        public string AuthFile { get; set; }
    }
}
