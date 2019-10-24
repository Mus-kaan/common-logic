//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.ImageBuilder
{
    public class ImageMetaInfo
    {
        [JsonProperty(PropertyName = "commit_id")]
        public string CommitId { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        public string TimeStamp { get; set; }

        [JsonProperty(PropertyName = "image_id")]
        public string ImageId { get; set; }

        [JsonProperty(PropertyName = "build_tag")]
        public string BuildTag { get; set; }
    }
}
