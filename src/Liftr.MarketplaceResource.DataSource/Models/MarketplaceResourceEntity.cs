//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.DataSource.Mongo;
using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Liftr.MarketplaceResource.DataSource.Models
{
    /// <summary>
    /// This entity will be used to store the Managed Identity and Marketplace metadata for a resource
    /// It will be used by the token server to generate authentication token needed by partner
    /// </summary>
    [BsonIgnoreExtraElements]
    public class MarketplaceResourceEntity : BaseResourceEntity, IMarketplaceResourceEntity
    {
        public MarketplaceResourceEntity(MarketplaceSubscription marketplaceSubscription, string saasResourceId, string resourceId, string tenantId)
        {
            if (string.IsNullOrEmpty(saasResourceId))
            {
                throw new System.ArgumentException("message", nameof(saasResourceId));
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                throw new System.ArgumentException("message", nameof(resourceId));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new System.ArgumentException("message", nameof(tenantId));
            }

            MarketplaceSubscription = marketplaceSubscription ?? throw new System.ArgumentNullException(nameof(marketplaceSubscription));
            SaasResourceId = saasResourceId;
            TenantId = tenantId;
            ResourceId = resourceId;
        }

        [BsonElement("mpsubId")]
        public MarketplaceSubscription MarketplaceSubscription { get; set; }

        [BsonElement("saasRid")]
        public string SaasResourceId { get; set; }
    }
}
