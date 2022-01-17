//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DBService.Contracts.Interfaces;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class BaseEntity : IEntity
    {
        protected BaseEntity()
        {
            EntityId = Guid.NewGuid().ToString();
        }

        [BsonId]
        public string Id { get; set; } // for liftr resource id & resource id is same

        [BsonElement("entityId")]
        public string EntityId { get; set; }

        [BsonElement("resourceName")]
        public string ResourceName { get; set; }

        [BsonElement("azSubsId")]
        public string AzSubsId { get; set; }

        [BsonElement("active")]
        public bool Active { get; set; } = true;

        [BsonElement("createdUTC")]
        public DateTime CreatedUTC { get; set; } = DateTime.UtcNow;

        [BsonElement("lastModifiedUTC")]
        public DateTime LastModifiedUTC { get; set; } = DateTime.UtcNow;

        [BsonElement("eTag")]
        public string ETag { get; set; }

        [BsonElement("resourceId")]
        public string ResourceId { get; set; } // ARM resource id;
    }
}