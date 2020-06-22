//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Marketplace.ARM.Contracts
{
    public class MarketplaceSaasTokenResponse
    {
        [JsonProperty("publisherOfferBaseUri")]
        public Uri PublisherOfferBaseUri { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
