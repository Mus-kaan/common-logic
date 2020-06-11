//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class MarketplaceSaasResourceEntity : IMarketplaceSaasResource
    {
        public MarketplaceSaasResourceEntity(
            MarketplaceSubscription marketplaceSubscription,
            string name,
            string plan,
            string billingTermId,
            BillingTermTypes billingTermType,
            int? quantity = null)
        {
            MarketplaceSubscription = marketplaceSubscription;
            Name = name;
            Plan = plan;
            BillingTermId = billingTermId;
            BillingTermType = billingTermType;
            Quantity = quantity;
        }

        [BsonElement("mp_sub")]
        [BsonSerializer(typeof(MarketplaceSubscriptionSerializer))]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("plan")]
        public string Plan { get; set; }

        [BsonElement("bill_tid")]
        public string BillingTermId { get; set; }

        [BsonElement("bill_ttype")]
        [BsonRepresentation(BsonType.String)]
        public BillingTermTypes BillingTermType { get; set; }

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
