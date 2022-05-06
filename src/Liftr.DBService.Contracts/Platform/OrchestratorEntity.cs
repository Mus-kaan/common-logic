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
using System.Linq;

namespace Microsoft.Liftr.DBService.Contracts.Platform
{
    public class OrchestratorEntity : BaseEntity
    {
        public OrchestratorEntity(string serviceTreeId, string repoName)
        {
            ServiceTreeId = string.IsNullOrWhiteSpace(serviceTreeId) ? throw new ArgumentNullException(nameof(serviceTreeId)) : serviceTreeId;
            RepoName = string.IsNullOrWhiteSpace(repoName) ? throw new ArgumentNullException(nameof(repoName)) : repoName;
        }

        [BsonElement("partnerName")]
        public string PartnerName { get; set; }

        [BsonElement("serviceTreeId")]
        public string ServiceTreeId { get; set; }

        [BsonElement("serviceTreeName")]
        public string ServiceTreeName { get; set; }

        [BsonElement("repoName")]
        public string RepoName { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [BsonElement("status")]
        public ExecutionStatus Status { get; set; } = ExecutionStatus.New;

        [BsonElement("executionResults")]
        public IDictionary<string, string> ExecutionResults { get; set; }

        [BsonElement("executionSteps")]
        public IEnumerable<ExecutionStep> ExecutionSteps { get; set; }

        public ExecutionStep GetExecutionStep(string stepName)
        {
            var executionStep = ExecutionSteps?.FirstOrDefault(p => p.Name.Equals(stepName, StringComparison.Ordinal));
            if (executionStep == null)
            {
                throw new InvalidOperationException($"Unable to find Pipeline Execution data for step {stepName}, serviceTreeName {ServiceTreeName}, repoName {RepoName}");
            }

            return executionStep;
        }
    }
}
