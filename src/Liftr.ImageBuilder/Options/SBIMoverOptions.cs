//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.ImageBuilder
{
    public class SBIMoverOptions
    {
        public IEnumerable<string> Versions { get; set; }

        public string Region { get; set; }

        public string SBIContainerName { get; set; } = "sbi-source-images";
    }
}
