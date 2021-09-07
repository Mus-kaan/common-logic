//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model
{
    public class DiagnosticSettingsModelList
    {
        [JsonProperty("value")]
        public List<DiagnosticSettingsModel> Value { get; set; }
    }

    public class DiagnosticSettingsModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("properties")]
        public DiagnosticSettingsPropertiesModel Properties { get; set; }
    }

    public class DiagnosticSettingsPropertiesModel
    {
        [JsonProperty("marketplacePartnerId")]
        public string MarketplacePartnerId { get; set; }

        [JsonProperty("logs")]
        public List<DiagnosticSettingsLogsOrMetricsModel> Logs { get; set; }

        [JsonProperty("metrics")]
        public List<DiagnosticSettingsLogsOrMetricsModel> Metrics { get; set; }
    }

    public class DiagnosticSettingsLogsOrMetricsModel
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
    }
}