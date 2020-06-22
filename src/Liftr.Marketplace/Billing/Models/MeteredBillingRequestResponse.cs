//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Text.Json.Serialization;

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
        [JsonPropertyName("code")]
        public string Code { get; set; }
    }
}
