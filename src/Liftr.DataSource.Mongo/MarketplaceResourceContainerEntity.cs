//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Liftr.DataSource.Mongo
{
    /// <inheritdoc/>
    public class MarketplaceResourceContainerEntity : BaseResourceEntity, IMarketplaceResourceContainerEntity
    {
        public MarketplaceResourceContainerEntity(MarketplaceSaasResourceEntity marketplaceSaasResource, string resourceId, string tenantId)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new System.ArgumentException("Resource id cannot be empty", nameof(resourceId));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new System.ArgumentException("Tenant id cannot be empty", nameof(tenantId));
            }

            MarketplaceSaasResource = marketplaceSaasResource ?? throw new System.ArgumentNullException(nameof(marketplaceSaasResource));
            TenantId = tenantId;
            ResourceId = resourceId;
        }

        [BsonElement("mp_res")]
        public MarketplaceSaasResourceEntity MarketplaceSaasResource { get; set; }
    }
}
