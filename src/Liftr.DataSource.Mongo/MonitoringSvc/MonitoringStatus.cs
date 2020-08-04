//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringStatus : MonitoringBaseEntity, IMonitoringStatus
    {
        [BsonElement("isMonitored")]
        public bool IsMonitored { get; set; }

        [BsonElement("reason")]
        public string Reason { get; set; }

        [BsonRequired]
        [BsonElement("lastModified")]
        public DateTime LastModifiedAtUTC { get; set; }
    }
}
