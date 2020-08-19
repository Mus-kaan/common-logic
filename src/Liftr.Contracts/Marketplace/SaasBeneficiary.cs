//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.Contracts.Marketplace
{
    // https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Models%2FAadEntity.cs&_a=contents&version=GBmaster
    public class SaasBeneficiary
    {
        [JsonProperty("emailId")]
        public string EmailId { get; set; }

        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("puid")]
        public string Puid { get; set; }
    }

    // https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Models%2FSaasV2%2FSubscriptionTerm.cs&_a=contents&version=GBmaster
    public class SubscriptionTerm
    {
        /// <summary>
        /// Gets or sets the term unit.
        /// </summary>
        [JsonProperty("termUnit")]
        public string TermUnit { get; set; }
    }

    public class MarketplaceSubscriptionDetails
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("offerId")]
        public string OfferId { get; set; }

        [JsonProperty("publisherId")]
        public string PublisherId { get; set; }

        [JsonProperty("beneficiary")]
        public SaasBeneficiary Beneficiary { get; set; }

        [JsonProperty("term")]
        public SubscriptionTerm Term { get; set; }

        [JsonProperty("quantity")]
        public int? Quantity { get; set; }
    }
}