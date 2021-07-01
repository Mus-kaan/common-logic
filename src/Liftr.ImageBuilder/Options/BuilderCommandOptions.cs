//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;
using Microsoft.Liftr.Contracts;

namespace Microsoft.Liftr.ImageBuilder
{
    public enum ActionType
    {
        BakeNewVersion,
        ImportOneVersion,
        OutputACRInformation,
    }

    public class BuilderCommandOptions
    {
        [Option('a', "action", Required = false, HelpText = "Action type, e.g. BakeNewVersion, ImportOneVersion, OutputACRInformation.")]
        public ActionType Action { get; set; } = ActionType.BakeNewVersion;

        [Option('f', "file", Required = true, HelpText = "Path to the configuration file.")]
        public string ConfigPath { get; set; }

        [Option("outputSubscriptionIdOnly", Required = false, HelpText = "Only write subscription Id to disk without any other work.")]
        public bool OutputSubscriptionIdOnly { get; set; } = false;

        [Option('n', "imageName", Required = true, HelpText = "Image name, the name of the generating Shared Image Galalery Image Name.")]
        public string ImageName { get; set; }

        [Option('v', "imageVersion", Required = true, HelpText = "The generating Shared Image Gallery Image Version Name, e.g. '0.9.2326'")]
        public string ImageVersion { get; set; }

        [Option("srcImg", Required = false, HelpText = "Source image type, one of: [ UbuntuServer1804, RedHat7LVM, CentOS, WindowsServer2016Datacenter, WindowsServer2016DatacenterCore, WindowsServer2016DatacenterContainers, WindowsServer2019Datacenter, WindowsServer2019DatacenterCore, WindowsServer2019DatacenterContainers, U1604LTS, U1604FIPS, U1804LTS ]")]
        public SourceImageType? SourceImage { get; set; } = null;

        [Option("cloud", Required = false, HelpText = "Azure cloud type, one of: [ Public, DogFood, Fairfax, Mooncake, USNat, USSec ]")]
        public CloudType Cloud { get; set; } = CloudType.Public;

        [Option("spnObjectId", Required = false, HelpText = "The Object Id of the executing Service Principal.")]
        public string RunnerSPNObjectId { get; set; }

        [Option("artifactPath", Required = false, HelpText = "Path to the artifact package file. The artifact must be packed in a tar file. We will call 'bake-image.sh' in the package.")]
        public string ArtifactPath { get; set; }

        [Option("authFile", Required = false, HelpText = "Use auth file to login instead of managed identity.")]
        public string AuthFile { get; set; }
    }
}
