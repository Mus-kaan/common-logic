//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringBaseEntity : IMonitoringBaseEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string DocumentObjectId { get; set; }

        /// <summary>
        /// Indexed, not unique.
        /// </summary>
        [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("parOId")]
        public string PartnerEntityId { get; set; }

        /// <summary>
        /// Indexed, not unique.
        /// </summary>
        [BsonRequired]
        [BsonElement("monitoredRId")]
        public string MonitoredResourceId { get; set; }

        /// <summary>
        /// Sharding (partition) key.
        /// </summary>
        [BsonRequired]
        [BsonElement("tenant")]
        public string TenantId { get; set; }
    }
}
