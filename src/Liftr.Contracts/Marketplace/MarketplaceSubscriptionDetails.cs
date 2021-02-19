//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Contracts.Marketplace
{
    // https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Models%2FSaasV2%2FSubscriptionStatusV2.cs&_a=contents&version=GBmaster
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SaasSubscriptionStatus
    {
        PendingFulfillmentStart,
        Subscribed,
        Suspended,
        Unsubscribed,
    }

    // https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Models%2FSaasV2%2FSubscriptionV2.cs&_a=contents&version=GBmaster
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

        [JsonProperty("saasSubscriptionStatus")]
        public virtual SaasSubscriptionStatus SaasSubscriptionStatus { get; set; }

        [JsonProperty("additionalMetadata")]
        public virtual SaasAdditionalMetadata AdditionalMetadata { get; set; }

        #region sample
        /*
         "subscription": {
        "doctype": "Subscription",
        "partitionKey": "bd0c8edb-451b-2894-c96f-30cbdd143b91",
        "created": "2020-08-18T23:15:15.7303576Z",
        "lastModified": "2020-08-18T23:15:17.8554215Z",
        "isDeleted": false,
        "additionalMetadata": {
            "AzureSubscriptionId": "b92d7765-5b31-4a44-a045-e12e58f4ab1f",
            "ResourceId": "90ea79e0-2081-4dff-9a85-52fbae4485b4"
        },
        "isMigration": false,
        "skuId": "0001",
        "isHidden": false,
        "id": "bd0c8edb-451b-2894-c96f-30cbdd143b91",
        "publisherId": "datadog1591740804488",
        "offerId": "datadog_v0",
        "name": "AzureMarketplaceDatadog_liftr-akshita-test-18-8-4-14_6a10f7a8-acca-49b4-a7e1-4aaaab346808",
        "saasSubscriptionStatus": "PendingFulfillmentStart",
        "beneficiary": {
            "emailId": "akagarw@microsoft.com",
            "objectId": "64e83dac-7517-42cd-9a16-31f24c3e5c15",
            "tenantId": "1e2e7f2e-25fb-43cf-9d1b-f17a1bc61edf",
            "puid": "10030000A5D03A4B",
            "iss": "https://sts.windows.net/1e2e7f2e-25fb-43cf-9d1b-f17a1bc61edf/"
        },
        "purchaser": {
            "emailId": "akagarw@microsoft.com",
            "objectId": "64e83dac-7517-42cd-9a16-31f24c3e5c15",
            "tenantId": "1e2e7f2e-25fb-43cf-9d1b-f17a1bc61edf",
            "puid": "10030000A5D03A4B",
            "iss": "https://sts.windows.net/1e2e7f2e-25fb-43cf-9d1b-f17a1bc61edf/"
        },
        "planId": "datadog_private_preview",
        "term": {
            "termUnit": "P1M"
        },
        "isTest": true,
        "isFreeTrial": false,
        "allowedCustomerOperations": [
            "Delete",
            "Update",
            "Read"
        ],
        "sessionId": "90ea79e0-2081-4dff-9a85-52fbae4485b4",
        "fulfillmentId": "0b857450-f9a6-41fd-b517-bb21f2c82c56",
        "storeFront": "StoreForLiftr",
        "sandboxType": "None",
        "sessionMode": "None"
    }
        */
        #endregion
    }

    public class SaasAdditionalMetadata
    {
        public string AzureSubscriptionId { get; set; }

        public string ResourceUri { get; set; }

        public string IsSubscriptionLevel { get; set; }
    }
}