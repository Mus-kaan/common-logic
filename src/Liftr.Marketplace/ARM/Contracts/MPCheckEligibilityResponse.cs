//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.Marketplace.ARM.Contracts
{
    public class MPCheckEligibilityResponse
    {
        [JsonProperty(PropertyName = "isEligible")]
        public bool IsEligible { get; set; }

        [JsonProperty(PropertyName = "errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
