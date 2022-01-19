//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Marketplace.ARM.Contracts
{
    using Newtonsoft.Json;

    /// <summary>
    /// SaaS Purchase payment validation details
    /// </summary>
    public class MPCheckEligibilityRequest
    {
        /// <summary>
        /// Gets or sets the Publisher Id
        /// </summary>
        [JsonProperty("publisherId")]
        public string PublisherId { get; set; }

        /// <summary>
        /// Gets or sets the Offer Id
        /// </summary>
        [JsonProperty("offerId")]
        public string OfferId { get; set; }

        /// <summary>
        /// Gets or sets the Plan Id
        /// </summary>
        [JsonProperty("planId")]
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets the Name, Optional param
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Term Id
        /// </summary>
        [JsonProperty("termId")]
        public string TermId { get; set; }

        /// <summary>
        /// Gets or sets the Quantity, Optional param
        /// </summary>
        [JsonProperty("quantity")]
        public int? Quantity { get; set; }

        /// <summary>
        /// Gets or sets the Azure Subscription Id
        /// </summary>
        [JsonProperty("paymentChannelMetadata")]
        public PaymentChannelMetadata PaymentChannelMetadata { get; set; }

        public bool IsValid()
        {
            return
                this != null &&
                !string.IsNullOrWhiteSpace(PublisherId) &&
                !string.IsNullOrWhiteSpace(OfferId) &&
                !string.IsNullOrWhiteSpace(PlanId) &&
                !string.IsNullOrWhiteSpace(TermId) &&
                PaymentChannelMetadata != null && !string.IsNullOrWhiteSpace(PaymentChannelMetadata.AzureSubscriptionId);
        }
    }
}
