//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System.Collections.Generic;

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

        public string OtherTags { get; set; }

        public SourceImageType SourceImageType { get; set; }

        public Dictionary<string, string> GenerateTags()
        {
            Dictionary<string, string> tags = new Dictionary<string, string>()
            {
                [nameof(ImageName)] = ImageName,
                [nameof(ImageVersion)] = ImageVersion,
                [nameof(CreatedAtUTC)] = CreatedAtUTC,
                [nameof(CopiedAtUTC)] = CopiedAtUTC,
                [nameof(SourceImageType)] = SourceImageType.ToString(),
            };

            if (!string.IsNullOrEmpty(OtherTags))
            {
                var extraTags = OtherTags.FromJson<Dictionary<string, string>>();
                foreach (var kvp in extraTags)
                {
                    tags[kvp.Key] = kvp.Value;
                }
            }

            return tags;
        }
    }
}
