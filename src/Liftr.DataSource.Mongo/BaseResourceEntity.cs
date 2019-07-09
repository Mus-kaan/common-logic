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
        /// Unique, indexed.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EntityId { get; set; }

        /// <summary>
        /// Indexed, shard key.
        /// </summary>
        [BsonElement("subscriptionId")]
        public string SubscriptionId { get; set; }

        [BsonElement("rg")]
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Unique, indexed.
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("loc")]
        public string Location { get; set; }

        [BsonElement("tags")]
        public string Tags { get; set; }

        [BsonElement("state")]
        public string ProvisioningState { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedUTC { get; set; } = LiftrDateTime.MinValue;
    }
}
