//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Net;

namespace Microsoft.Liftr.Marketplace.Contracts
{
    // Base class for all Marketplace Operation Responses
    // https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Models%2FSaasV2%2FBaseOperationResponse.cs&_a=contents&version=GBmaster
    public class BaseOperationResponse
    {
        public OperationStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the resource location
        /// </summary>
        [JsonProperty("resourceLocation")]
        public Uri ResourceLocation { get; set; }

        /// <summary>
        /// Gets or sets the id
        /// </summary>
        [JsonProperty("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the error status code
        /// </summary>
        [JsonProperty("errorStatusCode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public HttpStatusCode? ErrorStatusCode { get; set; }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty("subscription")]
        public MarketplaceSubscriptionDetails SubscriptionDetails { get; set; }

        #region Tenant Level sampleresponse
        /* Sample response
{
  "status": "Succeeded",
  "resourceLocation": "https://main.df.marketplaceapi.azure.net/api/saasresources/subscriptions/fed85dc3-5603-8290-6627-317648e175cc?api-version=2018-08-31",
  "id": "fed85dc3-5603-8290-6627-317648e175cc",
  "subscription": {
    "doctype": "Subscription",
    "partitionKey": "fed85dc3-5603-8290-6627-317648e175cc",
    "isDeleted": false,
    "additionalMetadata": {
      "AzureSubscriptionId": "52d42ba4-3473-4064-9f95-e780df01f6de",
      "ResourceId": "ba6ee816-7286-4fad-9f9f-ee301824db41",
      "ResourceUri": "/subscriptions/52d42ba4-3473-4064-9f95-e780df01f6de/providers/Microsoft.SaaS/saasresources/fed85dc3-5603-8290-6627-317648e175cc",
      "IsSubscriptionLevel": "False"
    },
    "isMigration": false,
    "skuId": "0004",
    "isHidden": true,
    "id": "fed85dc3-5603-8290-6627-317648e175cc",
    "publisherId": "datadog1591740804488",
    "offerId": "dd_liftr_v2",
    "name": "akshita17",
    "saasSubscriptionStatus": "PendingFulfillmentStart",
    "beneficiary": {
      "emailId": "billtest350046@hotmail.com",
      "objectId": "6c76483f-16ed-4fc2-b4d7-94e2d5c629cd",
      "tenantId": "b3e78b16-9d91-4ae8-adb5-f32951c2be79",
      "iss": "",
      "clienT-IP": "73.181.175.2:49968"
    },
    "purchaser": {
      "emailId": "billtest350046@hotmail.com",
      "objectId": "6c76483f-16ed-4fc2-b4d7-94e2d5c629cd",
      "tenantId": "b3e78b16-9d91-4ae8-adb5-f32951c2be79",
      "iss": "",
      "clienT-IP": "73.181.175.2:49968"
    },
    "planId": "payg",
    "term": {
      "termUnit": "P1M"
    },
    "autoRenew": true,
    "isTest": true,
    "isFreeTrial": false,
    "allowedCustomerOperations": [
      "Delete",
      "Update",
      "Read"
    ],
    "sessionId": "ba6ee816-7286-4fad-9f9f-ee301824db41",
    "fulfillmentId": "a4f34773-085a-4cb3-8be8-bb43788a57cb",
    "storeFront": "StoreForLiftr",
    "sandboxType": "None",
    "created": "2021-03-04T19:06:52.5783227Z",
    "lastModified": "2021-03-04T19:06:52.5783227Z",
    "sessionMode": "None"
  }
         */
        #endregion
        #region Subscription Level sampleresponse
        /* Sample response
{
    "status": "Succeeded",
    "resourceLocation": "https://main.df.marketplaceapi.azure.net/api/saasresources/subscriptions/638ee90e-61bf-f594-e2ee-254ad8ba50dd?api-version=2018-08-31",
    "id": "638ee90e-61bf-f594-e2ee-254ad8ba50dd",
    "subscription": {
        "doctype": "Subscription",
        "partitionKey": "638ee90e-61bf-f594-e2ee-254ad8ba50dd",
        "isDeleted": false,
        "additionalMetadata": {
            "AzureSubscriptionId": "d3c0b378-d50b-4ac7-ac42-b9aacc66f6c5",
            "ResourceId": "5074566a-2c1d-4b40-b559-5374ad99797c",
            "ResourceUri": "/subscriptions/d3c0b378-d50b-4ac7-ac42-b9aacc66f6c5/resourceGroups/rohit-test/providers/Microsoft.SaaS/resources/RawResponse-Log",
            "IsSubscriptionLevel": "True"
        },
        "isMigration": false,
        "skuId": "0002",
        "isHidden": true,
        "commerceReconciliation": {
            "lastRun": "2021-02-17T22:00:10.4701498Z",
            "status": "Ok",
            "internalStatus": "Ok",
            "message": "AssetState: 'Active', TransactionType: 'Fulfill', TransactionState: 'Pending', SubscriptionStatus: 'PendingFulfillmentStart'",
            "isNewSubscription": true
        },
        "id": "638ee90e-61bf-f594-e2ee-254ad8ba50dd",
        "publisherId": "isvtestuklegacy",
        "offerId": "liftr_cf_dev",
        "name": "RawResponse-Log",
        "saasSubscriptionStatus": "PendingFulfillmentStart",
        "beneficiary": {
            "emailId": "rohanand@microsoft.com",
            "objectId": "58d2e826-8d41-4f73-93aa-6744e882139a",
            "tenantId": "6457aa98-4dba-4966-a260-6fc215e8616a",
            "puid": "1003DFFD0027E540",
            "iss": "https://sts.windows-ppe.net/6457aa98-4dba-4966-a260-6fc215e8616a/",
            "clienT-IP": "147.243.154.42:12131"
        },
        "purchaser": {
            "emailId": "rohanand@microsoft.com",
            "objectId": "58d2e826-8d41-4f73-93aa-6744e882139a",
            "tenantId": "6457aa98-4dba-4966-a260-6fc215e8616a",
            "puid": "1003DFFD0027E540",
            "iss": "https://sts.windows-ppe.net/6457aa98-4dba-4966-a260-6fc215e8616a/",
            "clienT-IP": "147.243.154.42:12131"
        },
        "planId": "payg",
        "term": {
            "termUnit": "P1M"
        },
        "autoRenew": true,
        "isTest": true,
        "isFreeTrial": false,
        "allowedCustomerOperations": [
            "Delete",
            "Update",
            "Read"
        ],
        "sessionId": "5074566a-2c1d-4b40-b559-5374ad99797c",
        "fulfillmentId": "a37995dd-0b91-44e4-a909-036b6e69b0f6",
        "storeFront": "StoreForLiftr",
        "sandboxType": "None",
        "created": "2021-02-17T22:00:07.5757017Z",
        "lastModified": "2021-02-17T22:00:07.5757017Z",
        "sessionMode": "None"
    }
}
         */
        #endregion
    }
}
