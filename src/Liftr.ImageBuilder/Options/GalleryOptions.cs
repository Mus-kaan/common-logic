//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;

namespace Microsoft.Liftr.ImageBuilder
{
    public class GalleryOptions
    {
        public string PartnerName { get; set; }

        public string ShortPartnerName { get; set; }

        public EnvironmentType Environment { get; set; }

        public string LocationStr { get; set; }

        [JsonIgnore]
        public Region Location => Region.Create(LocationStr);

        public string GalleryBaseName { get; set; }

        public string ImageName { get; set; }

        public int ImageVersionTTLInDays { get; set; }
    }
}
