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

        [BsonElement("active")]
        public bool Active { get; set; } = true;

        [BsonElement("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [BsonElement("lastModifiedUtc")]
        public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;

        [BsonIgnoreIfDefault]
        [BsonElement("eTag")]
        public string ETag { get; set; }

        [BsonElement("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        [BsonElement("resourceId")]
        public string ResourceId { get; set; }
    }
}
