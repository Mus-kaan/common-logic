//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class MarketplaceRelationshipEntity : BaseResourceEntity
    {
        public MarketplaceRelationshipEntity(
            string entityId,
            string resourceId,
            string region,
            string tenantId,
            MarketplaceSubscription marketplaceSubscription)
        {
            if (string.IsNullOrEmpty(entityId))
            {
                throw new ArgumentException($"'{nameof(entityId)}' cannot be null or empty", nameof(entityId));
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentException($"'{nameof(resourceId)}' cannot be null or empty", nameof(resourceId));
            }

            if (string.IsNullOrEmpty(region))
            {
                throw new ArgumentException($"'{nameof(region)}' cannot be null or empty", nameof(region));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException($"'{nameof(tenantId)}' cannot be null or empty", nameof(tenantId));
            }

            MarketplaceSubscription = marketplaceSubscription ?? throw new ArgumentNullException(nameof(marketplaceSubscription));
            ResourceId = resourceId.ToUpperInvariant();
            Region = region.ToLowerInvariant();
            TenantId = tenantId;
            EntityId = entityId;
        }

        /// <summary>
        /// Marketplace subscription linked to the resource
        /// </summary>
        [BsonElement("mp_sub")]
        [BsonSerializer(typeof(MarketplaceSubscriptionSerializer))]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        /// <summary>
        /// Region of the Resource
        /// </summary>
        [BsonElement("region")]
        public string Region { get; set; }
    }
}
