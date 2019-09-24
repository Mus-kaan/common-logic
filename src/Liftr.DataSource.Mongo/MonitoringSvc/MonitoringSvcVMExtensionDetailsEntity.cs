//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringSvcVMExtensionDetailsEntity : IMonitoringSvcVMExtensionDetailsEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EntityId { get; set; }

        [BsonElement("monitoringSvcResourceProviderType")]
        public string MonitoringSvcResourceProviderType { get; set; }

        [BsonElement("extensionName")]
        public string ExtensionName { get; set; }

        [BsonElement("publisherName")]
        public string PublisherName { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("version")]
        public string Version { get; set; }

        [BsonElement("operatingSystem")]
        public string OperatingSystem { get; set; }

        [BsonElement("timestampUTC")]
        public DateTimeOffset TimestampUTC { get; set; }
    }
}
