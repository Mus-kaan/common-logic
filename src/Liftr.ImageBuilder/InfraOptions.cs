//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ImageBuilder
{
    public enum InfraType
    {
        BakeImage,
        ImportImage,
    }

    public class InfraOptions
    {
        public InfraType Type { get; set; }

        public bool CreateExportStorage { get; set; }

        public bool UseACR { get; set; }
    }
}
