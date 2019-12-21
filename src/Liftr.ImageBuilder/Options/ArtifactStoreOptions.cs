//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ImageBuilder
{
    public class ArtifactStoreOptions
    {
        public string ContainerName { get; set; } = "artifact-store";

        public double SASTTLInMinutes { get; set; } = 60;

        public double OldArtifactTTLInDays { get; set; } = 7;
    }
}
