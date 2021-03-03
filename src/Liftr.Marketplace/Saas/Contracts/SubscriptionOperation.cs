//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Contracts;
using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Marketplace.Saas.Contracts
{
    /// <summary>
    /// Operation used by the Marketplace and RP to communicate on the updates made to the subscription.
    /// <see href="https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2#get-operation-status">Documentation</see>
    /// <see href="https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Models%2FSaasV2%2FPublisherOperationV2.cs">Marketplace side Model</see>
    /// </summary>
    public class SubscriptionOperation : BaseOperationResponse
    {
        public string Action { get; set; }

        public Guid ActivityId { get; set; }

        public string OfferId { get; set; }

        public string OperationRequestSource { get; set; }

        public string PlanId { get; set; }

        public string PublisherId { get; set; }

        public string Quantity { get; set; }

        [JsonProperty("subscriptionId")]
        [JsonConverter(typeof(MarketplaceSubscriptionConverter))]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
