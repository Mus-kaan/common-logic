//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.Marketplace.Billing.Models
{
    public class MeteredBillingRequestResponse : AzureMarketplaceRequestResult
    {
        public MeteredBillingRequestResponse()
        {
            Code = "Ok";
        }

        /// <summary>
        /// Request status code in readable format
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
    }
}
