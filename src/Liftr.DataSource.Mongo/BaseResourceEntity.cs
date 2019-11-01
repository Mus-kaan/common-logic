//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public abstract class BaseResourceEntity : IResourceEntity
    {
        protected BaseResourceEntity()
        {
            EntityId = ObjectId.GenerateNewId().ToString();
        }

        /// <summary>
        /// Unique, indexed, shard key.
        /// Id of the entity. This is different from the ARM resource Id.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EntityId { get; set; }

        /// <summary>
        /// Indexed, not unique.
        /// ARM resource Id.
        /// </summary>
        [BsonElement("rid")]
        public string ResourceId { get; set; }

        [BsonElement("provisionState")]
        [BsonRepresentation(BsonType.String)]
        public ProvisioningState ProvisioningState { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedUTC { get; set; } = LiftrDateTime.MinValue;

        [BsonElement("etag")]
        public string ETag { get; set; }
    }
}
