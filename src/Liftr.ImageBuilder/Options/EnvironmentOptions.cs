//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;

namespace Microsoft.Liftr.ImageBuilder
{
    public class EnvironmentOptions : RunningEnvironmentOptions
    {
        public string AzureVMImageBuilderObjectId { get; set; }

        public string BaseSBIVerion { get; set; }

        public ArtifactStoreOptions ArtifactOptions { get; set; }

        public SBIMoverOptions SBIMoverOptions { get; set; }
    }
}
