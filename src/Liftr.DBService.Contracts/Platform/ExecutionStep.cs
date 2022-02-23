//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DBService.Contracts.Platform.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Liftr.DBService.Contracts.Platform
{
    public class ExecutionStep
    {
        public ExecutionStep(string name, string data)
        {
            StepName = string.IsNullOrWhiteSpace(name) ? throw new ArgumentNullException(nameof(name)) : name;
            Data = string.IsNullOrWhiteSpace(data) ? throw new ArgumentNullException(nameof(data)) : data;
        }

        [BsonElement("name")]
        public string StepName { get; set; }

        [BsonElement("description")]
        public string StepDescription { get; set; }

        [BsonElement("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public PlatformExecutionStatus Status { get; set; }

        [BsonElement("data")]
        public string Data { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedUTC { get; set; } = DateTime.UtcNow;

        [BsonElement("lastModified")]
        public DateTime LastModifiedUTC { get; set; } = DateTime.UtcNow;

        [BsonElement("hasChildSteps")]
        public bool HasChildSteps { get; set; }
    }
}
