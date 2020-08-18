//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    [SwaggerExtension(ExcludeFromSwagger = true)]
    public class MarketplaceSaasResourceEntity : IMarketplaceSaasResource
    {
        public MarketplaceSaasResourceEntity(
            MarketplaceSubscription marketplaceSubscription,
            string name,
            string planId,
            string offerId,
            string publisherId,
            string billingTermId,
            BillingTermTypes billingTermType,
            SaasBeneficiary beneficiary,
            int? quantity = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be empty", nameof(name));
            }

            if (string.IsNullOrEmpty(planId))
            {
                throw new ArgumentException("PlanId cannot be empty", nameof(planId));
            }

            if (string.IsNullOrEmpty(offerId))
            {
                throw new ArgumentException("OfferId cannot be empty", nameof(offerId));
            }

            if (string.IsNullOrEmpty(publisherId))
            {
                throw new ArgumentException("PublisherId cannot be empty", nameof(publisherId));
            }

            if (string.IsNullOrEmpty(billingTermId))
            {
                throw new ArgumentException("BillingTermId cannot be empty", nameof(billingTermId));
            }

            MarketplaceSubscription = marketplaceSubscription ?? throw new ArgumentNullException(nameof(marketplaceSubscription));
            Name = name;
            PlanId = planId;
            OfferId = offerId;
            PublisherId = publisherId;
            BillingTermId = billingTermId;
            BillingTermType = billingTermType;
            Quantity = quantity;
            Beneficiary = beneficiary ?? throw new ArgumentNullException(nameof(beneficiary));
        }

        [BsonElement("mp_sub")]
        [BsonSerializer(typeof(MarketplaceSubscriptionSerializer))]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("planId")]
        public string PlanId { get; set; }

        [BsonElement("offerId")]
        public string OfferId { get; set; }

        [BsonElement("publisherId")]
        public string PublisherId { get; set; }

        [BsonElement("bill_tid")]
        public string BillingTermId { get; set; }

        [BsonElement("bill_ttype")]
        [BsonRepresentation(BsonType.String)]
        public BillingTermTypes BillingTermType { get; set; }

        [BsonElement("beneficiary")]
        public SaasBeneficiary Beneficiary { get; set; }

        [BsonElement("quantity")]
        [BsonIgnoreIfNull]
        public int? Quantity { get; set; }
    }

    public class MarketplaceSubscriptionSerializer : IBsonSerializer<MarketplaceSubscription>
    {
        public Type ValueType => typeof(MarketplaceSubscription);

        public MarketplaceSubscription Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return MarketplaceSubscription.From(context.Reader.ReadString());
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MarketplaceSubscription value)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            context.Writer.WriteString(value.Id.ToString());
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            Serialize(context, args, value as MarketplaceSubscription);
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }
    }
}
