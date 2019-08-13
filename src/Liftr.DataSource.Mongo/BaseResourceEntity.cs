//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

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
        /// Id of the entity. This is different from the ARM resource Id.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EntityId { get; set; }

        /// <summary>
        /// Indexed, shard key.
        /// Subscription Id
        /// </summary>
        [BsonElement("subscriptionId")]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Resource Group
        /// </summary>
        [BsonElement("rg")]
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Unique, indexed.
        /// The name of the resource.
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; }

        /// <summary>
        /// The type of the resource.
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; }

        /// <summary>
        /// The location of the resource.
        /// </summary>
        [BsonElement("loc")]
        public string Location { get; set; }

        /// <summary>
        /// The tags of the resource.
        /// </summary>
        [BsonElement("tags")]
        public IDictionary<string, string> Tags { get; set; }

        [BsonElement("provisionState")]
        [BsonRepresentation(BsonType.String)]
        public ProvisioningState ProvisioningState { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedUTC { get; set; } = LiftrDateTime.MinValue;
    }
}
