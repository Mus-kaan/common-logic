//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringSvcMonitoredEntity : IMonitoringSvcMonitoredEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EntityId { get; set; }

        [BsonElement("monitorableResourceId")]
        public string MonitoredResourceId { get; set; }

        [BsonElement("monitoringResourceId")]
        public string MonitoringResourceId { get; set; }

        [BsonElement("resourceType")]
        public string ResourceType { get; set; }

        [BsonElement("partnerServiceType")]
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public MonitoringSvcType PartnerServiceType { get; set; }

        [BsonElement("partnerCredential")]
        public string PartnerCredential { get; set; }

        [BsonElement("priority")]
        public uint Priority { get; set; }

        [BsonElement("enabled")]
        public bool Enabled { get; set; }

        [BsonElement("timestampUTC")]
        public DateTimeOffset TimestampUTC { get; set; }

        [BsonElement("encryptionMetaData")]
        [JsonConverter(typeof(IEncryptionMetaData))]
        public IEncryptionMetaData EncryptionMetaData { get; set; }
    }
}
