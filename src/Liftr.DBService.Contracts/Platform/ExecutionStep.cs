//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DBService.Contracts.Platform.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.DBService.Contracts.Platform
{
    public class ExecutionStep
    {
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public ExecutionStatus Status { get; set; }

        [BsonElement("data")]
        public string Data { get; set; }

        [BsonElement("result")]
        public string Result { get; set; }

        [BsonElement("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [BsonElement("lastModifiedUtc")]
        public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;

        [BsonElement("hasChildSteps")]
        public bool HasChildSteps { get; set; }

        [BsonElement("executionSteps")]
        public IEnumerable<ExecutionStep> ExecutionSteps { get; set; }
    }
}
