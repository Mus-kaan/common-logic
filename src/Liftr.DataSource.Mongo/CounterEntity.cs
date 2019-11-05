//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class CounterEntity : ICounterEntity
    {
        public CounterEntity()
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
        /// Key of the counter. This will be unique and indexed.
        /// </summary>
        [BsonElement("key")]
        public string CounterKey { get; set; }

        [BsonElement("val")]
        public int CounterValue { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedUTC { get; set; }

        [BsonElement("lastModified")]
        public DateTime LastModifiedUTC { get; set; }
    }
}
