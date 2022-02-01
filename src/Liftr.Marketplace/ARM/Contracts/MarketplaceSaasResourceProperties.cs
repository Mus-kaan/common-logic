//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.Marketplace.ARM.Contracts
{
    public class MarketplaceSaasResourceProperties
    {
        public string PublisherId { get; set; }

        public string OfferId { get; set; }

        public string Name { get; set; }

        public string PlanId { get; set; }

        public string PaymentChannelType { get; set; }

        public string TermId { get; set; }

        [JsonProperty("quantity")]
        public int? Quantity { get; set; }

        public PaymentChannelMetadata PaymentChannelMetadata { get; set; }

        // [TO DO: Add Regex Validation Check for SaaS resource name when implemented by MP team]
        public bool IsValid()
        {
            return
                !string.IsNullOrEmpty(PublisherId) &&
                !string.IsNullOrEmpty(OfferId) &&
                !string.IsNullOrEmpty(Name) &&
                !string.IsNullOrEmpty(PlanId) &&
                !string.IsNullOrEmpty(TermId) &&
                PaymentChannelMetadata != null &&
                !string.IsNullOrEmpty(PaymentChannelMetadata.AzureSubscriptionId);
        }
    }

    public class PaymentChannelMetadata
    {
        [JsonProperty("AzureSubscriptionId")]
        public string AzureSubscriptionId { get; set; }

        [JsonProperty("ResourceGroup")]
        public string ResourceGroup { get; set; }
    }
}
