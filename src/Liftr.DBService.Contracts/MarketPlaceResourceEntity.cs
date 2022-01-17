//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class MarketPlaceResourceEntity : BaseEntity
    {
        public MarketPlaceResourceEntity(string liftrResourceId, string mpId)
        {
            ResourceId = liftrResourceId;
            Id = mpId;
        }

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
        public DateTime StartDate { get; set; }

        [BsonElement("endDate")]
        public DateTime EndDate { get; set; }

        [BsonElement("quantity")]
        public int Quantity { get; set; }

        [BsonElement("saasSubscriptionStatus")]
        public SaasSubscriptionStatus SaasSubscriptionStatus { get; set; }

        [BsonElement("billingTermType")]
        public BillingTermTypes BillingTermType { get; set; }

        [BsonElement("resourceUri")]
        public Uri ResourceUri { get; set; }
    }
}