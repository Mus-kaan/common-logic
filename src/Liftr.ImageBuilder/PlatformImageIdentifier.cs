//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.ImageBuilder
{
    public class PlatformImageIdentifier
    {
        /// <summary>
        /// Gets or sets the name of the gallery Image Definition publisher.
        /// </summary>
        [JsonProperty(PropertyName = "publisher")]
        public string Publisher { get; set; }

        /// <summary>
        /// Gets or sets the name of the gallery Image Definition offer.
        /// </summary>
        [JsonProperty(PropertyName = "offer")]
        public string Offer { get; set; }

        /// <summary>
        /// Gets or sets the name of the gallery Image Definition SKU.
        /// </summary>
        [JsonProperty(PropertyName = "sku")]
        public string Sku { get; set; }

        /// <summary>
        /// Gets or sets the name of the gallery Image Definition version.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }
    }
}
