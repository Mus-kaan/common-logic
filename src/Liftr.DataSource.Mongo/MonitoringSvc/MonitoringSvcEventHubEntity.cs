//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringSvcEventHubEntity : IMonitoringSvcEventHubEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EntityId { get; set; }

        [BsonElement("partnerServiceType")]
        public MonitoringSvcType PartnerServiceType { get; set; }

        [BsonElement("dataType")]
        public MonitoringSvcDataType DataType { get; set; }

        [BsonElement("nameSpace")]
        public string Namespace { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("location")]
        public string Location { get; set; }

        [BsonElement("eventHubConnStr")]
        public string EventHubConnStr { get; set; }

        [BsonElement("storageConnStr")]
        public string StorageConnStr { get; set; }

        [BsonElement("authorizationRuleId")]
        public string AuthorizationRuleId { get; set; }

        [BsonElement("enabled")]
        public bool Enabled { get; set; }

        [BsonElement("timestampUTC")]
        public DateTimeOffset TimestampUTC { get; set; }
    }
}
