//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Contracts;
using Newtonsoft.Json;

namespace Microsoft.Liftr.Marketplace.ARM.Contracts
{
#nullable disable

    // https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Models%2FSaasV2%2FSubscriptionV2.cs&_a=contents&version=GBmaster
    public class SaasCreationResponse : MarketplaceAsyncOperationResponse
    {
        [JsonProperty("subscription")]
        public MarketplaceSubscriptionDetails SubscriptionDetails { get; set; }

        #region sampleresponse
        /* Sample response
{
    "status": "Succeeded",
    "resourceLocation": "https://marketplaceapi.microsoft.com/api/saasresources/subscriptions/bd0c8edb-451b-2894-c96f-30cbdd143b91?api-version=2018-08-31",
    "id": "bd0c8edb-451b-2894-c96f-30cbdd143b91",
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
}
         */
        #endregion
    }
}
