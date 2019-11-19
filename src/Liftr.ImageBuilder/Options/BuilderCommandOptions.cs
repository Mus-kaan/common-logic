//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;

namespace Microsoft.Liftr.ImageBuilder
{
    public enum ActionType
    {
        CreateOrUpdateImageGalleryResources,
        MoveSBIToOurStorage,
        GenerateCustomizedSBI,
        GenerateCustomizedWindowsBaseImage,
        CreateOrUpdateWindowsImageGalleryResources,
    }

    public class BuilderCommandOptions
    {
        [Option('a', "action", Required = true, HelpText = "Action type, one of: [ CreateOrUpdateImageGalleryResources, MoveSBIToOurStorage, GenerateCustomizedSBI, GenerateCustomizedWindowsBaseImage, CreateOrUpdateWindowsImageGalleryResources ]")]
        public ActionType Action { get; set; }

        [Option('f', "file", Required = true, HelpText = "Path to the configuration file.")]
        public string ConfigPath { get; set; }

        [Option('s', "subscription", Required = true, HelpText = "The target subscription Id.")]
        public string SubscriptionId { get; set; }

        [Option("authFile", Required = false, HelpText = "Use auth file to login instead of managed identity.")]
        public string AuthFile { get; set; }

        [Option("artifactPath", Required = false, HelpText = "Path to the artifact package file. The artifact must be packed in a tar file. We will call 'bake-image.sh' in the package.")]
        public string ArtifactPath { get; set; }

        [Option("imageMetaPath", Required = false, HelpText = "Path to the 'image-meta.json'.")]
        public string ImageMetaPath { get; set; }
    }
}
