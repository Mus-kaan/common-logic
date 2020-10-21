//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    [BsonIgnoreExtraElements]
    public class StorageEntity : IStorageEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string DocumentObjectId { get; set; }

        [BsonElement("accountName")]
        public string AccountName { get; set; }

        [BsonElement("resourceId")]
        public string ResourceId { get; set; }

        [BsonElement("lfloc")]
        public string LogForwarderRegion { get; set; }

        [BsonElement("stloc")]
        public string StorageRegion { get; set; }

        [BsonElement("priority")]
        [BsonRepresentation(BsonType.String)]
        public StoragePriority Priority { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAtUTC { get; set; }

        [BsonElement("active")]
        public bool Active { get; set; } = true;

        [BsonElement("ingestionEnabled")]
        public bool IngestionEnabled { get; set; } = true;

        [BsonElement("ver")]
        public string Version { get; set; } = "v1";
    }
}
