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
