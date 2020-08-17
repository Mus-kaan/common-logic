//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Contracts;
using Newtonsoft.Json;

namespace Microsoft.Liftr.Marketplace.ARM.Contracts
{
#nullable disable

    // https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Models%2FSaasV2%2FSubscriptionV2.cs&_a=contents&version=GBmaster
    public class SaasCreationResponse : MarketplaceAsyncOperationResponse
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
        public Beneficiary Beneficiary { get; set; }

        [JsonProperty("term")]
        public SubscriptionTerm Term { get; set; }

        #region sampleresponse
        /* Sample response
                    "doctype": "Subscription",
                    "partitionKey": "f3eabe68-37b7-df01-45a2-418fb81469bc",
                    "created": "2020-03-30T21:15:19.184622Z",
                    "lastModified": "0001-01-01T00:00:00",
                    "isDeleted": false,
                    "additionalMetadata": {
                        "AzureSubscriptionId": "f5f53739-49e7-49a4-b5b1-00c63a7961a1",
                        "ResourceId": "44da49f3-23af-453d-aaf3-69e50e4cb30f"
                    },
                    "isMigration": false,
                    "id": "f3eabe68-37b7-df01-45a2-418fb81469bc",
                    "publisherId": "isvtestuklegacy",
                    "offerId": "liftrtest2-preview",
                    "name": "akshita2_14",
                    "saasSubscriptionStatus": "Unsubscribed",
                    "beneficiary": {
                        "emailId": "akagarw@microsoft.com",
                        "objectId": "c6ec8275-0d83-4f4e-88b9-be97b046785a",
                        "tenantId": "72f988bf-86f1-41af-91ab-2d7cd011db47",
                        "puid": "10030000A5D03A4B",
                        "iss": "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/",
                        "idp": "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/"
                    },
                    "purchaser": {
                        "emailId": "akagarw@microsoft.com",
                        "objectId": "c6ec8275-0d83-4f4e-88b9-be97b046785a",
                        "tenantId": "72f988bf-86f1-41af-91ab-2d7cd011db47",
                        "puid": "10030000A5D03A4B",
                        "iss": "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/",
                        "idp": "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/"
                    },
                    "planId": "basic",
                    "term": {
                        "startDate": "2020-03-30T00:00:00Z",
                        "endDate": "2020-04-29T00:00:00Z",
                        "termUnit": "P1M"
                    },
                    "isTest": false,
                    "isFreeTrial": false,
                    "allowedCustomerOperations": [
                        "Delete",
                        "Update",
                        "Read"
                    ],
                    "sessionId": "44da49f3-23af-453d-aaf3-69e50e4cb30f",
                    "fulfillmentId": "48407389-c5cd-4c8f-8546-668f28d7ebc1",
                    "storeFront": "AzurePortal",
                    "sandboxType": "None",
                    "sessionMode": "None"
                    }
         */
        #endregion
    }

    // https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Models%2FAadEntity.cs&_a=contents&version=GBmaster
    public class Beneficiary
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
}
