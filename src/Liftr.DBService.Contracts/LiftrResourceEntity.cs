//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class LiftrResourceEntity : BaseRpEntity
    {
        public LiftrResourceEntity(string liftrResourceId)
        {
            ResourceId = liftrResourceId;
        }

        [BsonElement("resourceType")]
        public string ResourceType { get; set; }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("resourceGroup")]
        public string ResourceGroup { get; set; }

        [BsonElement("region")]
        public string Region { get; set; }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("provisioningState")]
        public string ProvisioningState { get; set; }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("userDetail")]
        public UserDetail UserDetail { get; set; }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("mpResourceId")]
        public string MarketplaceResourceId { get; set; }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("partnerResourceId")]
        public string PartnerResourceId { get; set; }
    }
}