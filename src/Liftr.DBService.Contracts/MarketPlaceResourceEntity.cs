//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class MarketPlaceResourceEntity : BaseRpEntity
    {
        public MarketPlaceResourceEntity(string liftrResourceId, string mpResourceId)
        {
            ResourceId = liftrResourceId;
            MarketPlaceResourceId = mpResourceId;
        }

        [BsonElement("mpResourceid")]
        public string MarketPlaceResourceId { get; set; }

        [BsonElement("publisherId")]
        public string PublisherId { get; set; }

        [BsonElement("offerId")]
        public string OfferId { get; set; }

        [BsonElement("planId")]
        public string PlanId { get; set; }

        [BsonElement("beneficiaryEmailId")]
        public string BeneficiaryEmailId { get; set; }

        [BsonElement("termUnit")]
        public string TermUnit { get; set; }

        [BsonElement("startDate")]
        public DateTime? StartDate { get; set; }

        [BsonElement("endDate")]
        public DateTime? EndDate { get; set; }

        [BsonElement("quantity")]
        public int? Quantity { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [BsonElement("saasSubscriptionStatus")]
        public SaasSubscriptionStatus? SaasSubscriptionStatus { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [BsonElement("billingTermType")]
        public BillingTermTypes? BillingTermType { get; set; }

        [BsonElement("resourceUri")]
        public Uri ResourceUri { get; set; }
    }
}