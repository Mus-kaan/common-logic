//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.ImageBuilder
{
    public class ContentStoreOptions
    {
        public string ArtifactContainerName { get; set; } = "artifact-store";

        public string VHDExportContainerName { get; set; } = "exporting-vhds";

        public string VHDImportContainerName { get; set; } = "importing-vhds";

        public string SourceSBIContainerName { get; set; } = "sbi-source-images";

        public double SASTTLInMinutes { get; set; } = 60;

        public double ContentTTLInDays { get; set; } = 7;

        public double ExportingVHDContainerSASTTLInDays { get; set; } = 5;

        public void ValidateValues()
        {
            if (ExportingVHDContainerSASTTLInDays > 7)
            {
                throw new InvalidOperationException($"Max {nameof(ExportingVHDContainerSASTTLInDays)} is 7 days");
            }

            double maxMinutes = 7 * 24 * 60;
            if (SASTTLInMinutes > maxMinutes)
            {
                throw new InvalidOperationException($"Max {nameof(SASTTLInMinutes)} is {maxMinutes} days");
            }
        }
    }
}
