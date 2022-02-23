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

        [BsonElement("resourceGroup")]
        public string ResourceGroup { get; set; }

        [BsonElement("region")]
        public string Region { get; set; }

        [BsonElement("provisioningState")]
        public string ProvisioningState { get; set; }

        [BsonElement("userDetail")]
        public UserDetail UserDetail { get; set; }

        [BsonElement("mpResourceId")]
        public string MarketplaceResourceId { get; set; }

        [BsonElement("partnerResourceId")]
        public string PartnerResourceId { get; set; }
    }
}