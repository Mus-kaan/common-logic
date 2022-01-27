//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DBService.Contracts.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class BaseEntity : IEntity
    {
        protected BaseEntity()
        {
            Id = ObjectId.GenerateNewId().ToString();
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("resourceName")]
        public string ResourceName { get; set; }

        [BsonElement("azSubsId")]
        public string AzSubsId { get; set; }

        [BsonElement("tenantId")]
        public string TenantId { get; set; }

        [BsonElement("active")]
        public bool Active { get; set; } = true;

        [BsonElement("createdUTC")]
        public DateTime CreatedUTC { get; set; } = DateTime.UtcNow;

        [BsonElement("lastModifiedUTC")]
        public DateTime LastModifiedUTC { get; set; } = DateTime.UtcNow;

        [BsonElement("eTag")]
        public string ETag { get; set; }

        [BsonElement("armResourceId")]
        public string ResourceId { get; set; } // ARM resource id;

        [BsonElement("workflowType")]
        public WorkflowTypeEnum WorkflowType { get; set; } // either through create flow or linking;

        [BsonElement("isDeleted")]
        public bool IsDeleted { get; set; } = false;
    }
}