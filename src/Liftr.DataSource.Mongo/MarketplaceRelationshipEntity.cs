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
        public MarketplaceRelationshipEntity(MarketplaceSubscription marketplaceSubscription, string resourceId, string region, string tenantId)
        {
            MarketplaceSubscription = marketplaceSubscription ?? throw new ArgumentNullException(nameof(marketplaceSubscription));
            ResourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
            RPRegion = region ?? throw new ArgumentNullException(nameof(region));
            TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        }

        /// <summary>
        /// Marketplace subscription linked to the resource
        /// </summary>
        [BsonElement("mp_sub")]
        [BsonSerializer(typeof(MarketplaceSubscriptionSerializer))]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        /// <summary>
        /// Region of the Resource Provider that provisions the resource
        /// </summary>
        [BsonElement("rp_region")]
        public string RPRegion { get; set; }
    }
}
