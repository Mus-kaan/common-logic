//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringRelationship : MonitoringBaseEntity, IMonitoringRelationship
    {
        [BsonElement("authRuleId")]
        public string AuthorizationRuleId { get; set; }

        [BsonElement("ehName")]
        public string EventhubName { get; set; }

        [BsonElement("diagSet")]
        public string DiagnosticSettingsName { get; set; }

        [BsonRequired]
        [BsonElement("createdAt")]
        public DateTime CreatedAtUTC { get; set; }
    }
}
