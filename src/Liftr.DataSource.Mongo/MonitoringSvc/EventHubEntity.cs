//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class EventHubEntity : IEventHubEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string DocumentObjectId { get; set; }

        [BsonElement("rp")]
        [BsonRepresentation(BsonType.String)]
        public MonitoringResourceProvider ResourceProvider { get; set; }

        [BsonElement("ns")]
        public string Namespace { get; set; }

        [BsonElement("ehName")]
        public string Name { get; set; }

        [BsonElement("loc")]
        public string Location { get; set; }

        [BsonElement("ehConnStr")]
        public string EventHubConnectionString { get; set; }

        [BsonElement("storConnStr")]
        public string StorageConnectionString { get; set; }

        [BsonElement("authRuleId")]
        public string AuthorizationRuleId { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAtUTC { get; set; }

        [BsonElement("ingestionEnabled")]
        public bool IngestionEnabled { get; set; } = true;

        [BsonElement("active")]
        public bool Active { get; set; } = true;
    }
}
