//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Marketplace.ARM.Contracts
{
    using Newtonsoft.Json;

    /// <summary>
    /// SaaS Purchase payment validation details
    /// </summary>
    public class PaymentValidationRequest
    {
        /// <summary>
        /// Gets or sets the Azure Subscription Id
        /// </summary>
        [JsonProperty("azureSubscriptionId")]
        public string AzureSubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the Tenant Id
        /// </summary>
        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the Term Id
        /// </summary>
        [JsonProperty("termId")]
        public string TermId { get; set; }

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
        /// Gets or sets the Publisher Id
        /// </summary>
        [JsonProperty("publisherId")]
        public string PublisherId { get; set; }

        /// <summary>
        /// Gets or sets the Email
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        public bool IsValid()
        {
            return
                !string.IsNullOrEmpty(PublisherId) &&
                !string.IsNullOrEmpty(OfferId) &&
                !string.IsNullOrEmpty(Email) &&
                !string.IsNullOrEmpty(PlanId) &&
                !string.IsNullOrEmpty(TermId) &&
                !string.IsNullOrEmpty(AzureSubscriptionId) &&
                !string.IsNullOrEmpty(TenantId);
        }
    }
}
