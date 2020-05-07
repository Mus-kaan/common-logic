//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ImageBuilder
{
    /// <summary>
    /// Storing meta data for the VHD blob.
    /// Although blob can store meta data in itself,
    /// however, Teleport will not move user metadata.
    /// Hence, we need to store the meta data explicity in a separate JSON.
    /// </summary>
    public class VHDMeta
    {
        public string ImageName { get; set; }

        public string ImageVersion { get; set; }

        public string CreatedAtUTC { get; set; }

        public string CopiedAtUTC { get; set; }

        public string ContentHash { get; set; }
    }
}
