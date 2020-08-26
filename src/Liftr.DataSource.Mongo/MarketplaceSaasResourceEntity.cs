//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    [SwaggerExtension(ExcludeFromSwagger = true)]
    public class MarketplaceSaasResourceEntity
    {
        public MarketplaceSaasResourceEntity(
            MarketplaceSubscription marketplaceSubscription,
            MarketplaceSubscriptionDetails subscriptionDetails,
            BillingTermTypes billingTermType)
        {
            MarketplaceSubscription = marketplaceSubscription ?? throw new ArgumentNullException(nameof(marketplaceSubscription));
            SubscriptionDetails = subscriptionDetails;
            BillingTermType = billingTermType;
        }

        [BsonId]
        [BsonElement("mp_sub")]
        [BsonSerializer(typeof(MarketplaceSubscriptionSerializer))]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        [BsonElement("mp_sub_det")]
        public MarketplaceSubscriptionDetails SubscriptionDetails { get; set; }

        [BsonElement("bill_ttype")]
        [BsonRepresentation(BsonType.String)]
        public BillingTermTypes BillingTermType { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedUTC { get; set; } = LiftrDateTime.MinValue;

        [BsonElement("lastModified")]
        public DateTime LastModifiedUTC { get; set; } = LiftrDateTime.MinValue;

        /// <summary>
        /// When the entity is deleted, this will be marked as false.
        /// The actual deletion happened after a fixed time interval.
        /// </summary>
        [BsonElement("active")]
        public bool Active { get; set; } = true;
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
